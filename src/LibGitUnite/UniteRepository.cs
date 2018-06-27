using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LibGit2Sharp;

namespace LibGitUnite
{
    public class UniteRepository : IDisposable
    {
        private readonly OptionFlags _options;
        private const string Separator = "\\";
        private List<DirectoryInfo> _folderInfo = new List<DirectoryInfo>();
        private readonly Repository _gitRepository;
        private readonly DirectoryInfo _gitDirectoryInfo;
        private static readonly FieldInfo FullNameField = typeof(FileSystemInfo).GetField("FullPath", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// Extended LibGit2Sharp.Repository with the Unite version of the Move command
        /// </summary>
        /// <param name = "path">
        ///   The path to the git repository to open, can be either the path to the git directory (for non-bare repositories this
        ///   would be the ".git" folder inside the working directory) or the path to the working directory.
        /// </param>
        /// <param name="options">Runtime command line options specified</param>
        public UniteRepository(string path, OptionFlags options)
        {
            _gitDirectoryInfo = new DirectoryInfo(path);
            _gitRepository = new Repository(GetFullName(_gitDirectoryInfo));
            _options = options;
        }

        public UniteRepository Process()
        {
            return GetHostDirectoryInfo().UniteFolderCasing().UniteFilenameCasing();
        }

        /// <summary>
        /// Modified version of LibGit2Sharp.Index.Move without the OS file system checks to unite file with proper case path
        /// </summary>
        /// <param name = "sourcePath">The path of the file within the working directory which has to be renamed.</param>
        /// <param name = "destinationPath">The target path of the file within the working directory.</param>
        public void Unite(string sourcePath, string destinationPath)
        {
            if (_options.HasFlag(OptionFlags.DryRun))
            {
                if (_options.HasFlag(OptionFlags.RenameEntriesInHostOS))
                {
                    Console.WriteLine("proposed rename local file: {0} -> {1}", destinationPath, sourcePath);
                }
                else
                {
                    Console.WriteLine("proposed rename: {0} -> {1}", sourcePath, destinationPath);
                }
                return;
            }

            try
            {
                if (_options.HasFlag(OptionFlags.RenameEntriesInHostOS))
                {
                    File.Move(destinationPath, sourcePath);
                    Console.WriteLine("rename local file: {0} -> {1}", destinationPath, sourcePath);
                }
                else
                {
                    _gitRepository.Index.Remove(sourcePath.Replace(_gitRepository.Info.WorkingDirectory, string.Empty));
                    _gitRepository.Index.Add(destinationPath.Replace(_gitRepository.Info.WorkingDirectory, string.Empty));
                    _gitRepository.Index.Write();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("error changing: {0} -> {1} [{2}]", sourcePath, destinationPath, ex.Message);
            }
        }

        /// <summary>
        /// Modified version of LibGit2Sharp.Index.Move without the OS file system checks to unite file with proper case path
        /// </summary>
        /// <param name = "indexChanges">The path changes required.</param>
        public void Unite(Dictionary<string, string> indexChanges)
        {
            if (!indexChanges.Any()) return;

            if (_options.HasFlag(OptionFlags.DryRun))
            {
                foreach (var indexChange in indexChanges)
                {
                    if (_options.HasFlag(OptionFlags.RenameEntriesInHostOS))
                    {
                        Console.WriteLine("proposed rename local directory: {0} -> {1}", indexChange.Value, indexChange.Key);
                    }
                    else
                    {
                        Console.WriteLine("proposed rename: {0} -> {1}", indexChange.Key, indexChange.Value);
                    }
                }
                return;
            }

            if (_options.HasFlag(OptionFlags.RenameEntriesInHostOS))
            {
                foreach (var indexChange in indexChanges)
                {
                    Directory.Move(indexChange.Value, indexChange.Key);
                    Console.WriteLine("rename local directory: {0} -> {1}", indexChange.Value, indexChange.Key);
                }
            }
            else 
            {
                RemoveIndexEntries(indexChanges);
                AddIndexEntries(indexChanges);
                _gitRepository.Index.Write();
            }
        }

        private void AddIndexEntries(Dictionary<string, string> indexChanges)
        {
            foreach (var indexChange in indexChanges)
            {
                try
                {
                    _gitRepository.Index.Add(indexChange.Value.Replace(_gitRepository.Info.WorkingDirectory, string.Empty));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("error changing: {0} -> {1} [{2}]", indexChange.Key, indexChange.Value, ex.Message);
                }
            }
        }

        private void RemoveIndexEntries(Dictionary<string, string> indexChanges)
        {
            foreach (var indexChange in indexChanges)
            {
                try
                {
                    _gitRepository.Index.Remove(indexChange.Key.Replace(_gitRepository.Info.WorkingDirectory, string.Empty));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("error changing: {0} -> {1} [{2}]", indexChange.Key, indexChange.Value, ex.Message);
                }
            }
        }

        /// <summary>
        /// Get the FullName from long file names using the hidden property FullPath
        /// </summary>
        /// <param name="fsi"></param>
        /// <returns>Full Name Field</returns>
        private static string GetFullName(FileSystemInfo fsi)
        {
            return (string)FullNameField.GetValue(fsi);
        }

        /// <summary>
        /// Builds a list of directory names as seen by the host operating system
        /// </summary>
        /// <returns>A <see cref="UniteRepository"/> aware of directories reported by host OS</returns>
        private UniteRepository GetHostDirectoryInfo()
        {
            try
            {
                _folderInfo = _gitDirectoryInfo.EnumerateDirectories("*", SearchOption.AllDirectories)
                .Where(d => !GetFullName(d).ToLowerInvariant().StartsWith(_gitRepository.Info.Path.TrimEnd('\\').ToLowerInvariant()))
                .ToList();
            }
            catch (Exception ex)
            {
                Console.Write("Git.Unite: ");
                Console.WriteLine(ex.Message);
            }

            return this;
        }

        /// <summary>
        /// Unite directory name casing between git index and host OS
        /// </summary>
        private UniteRepository UniteFolderCasing()
        {
            if (!_options.HasFlag(OptionFlags.UniteDirectories) || !_folderInfo.Any()) return this;

            var foldersFullPathMap = new HashSet<string>(_folderInfo.ConvertAll(GetFullName));

            // Find all repository files with directory paths not found in the host OS folder collection
            var indexEntries =
                _gitRepository.Index.Where(f => f.Path.LastIndexOf(Separator, StringComparison.Ordinal) != -1
                                      &&
                                      !foldersFullPathMap.Any(s => s.Contains(f.Path.Substring(0, f.Path.LastIndexOf(Separator, StringComparison.Ordinal)))));

            // Build manifest of directory changes in order to remove all entries first before adding due to git internal handling of adds with existing directory entries
            var indexChanges = new Dictionary<string, string>();

            // Unite the casing of the repository file directory path with the casing seen by the host operating system
            foreach (var entry in indexEntries)
            {
                var lastIndexOf = entry.Path.LastIndexOf(Separator, StringComparison.Ordinal);
                var filename = entry.Path.Substring(lastIndexOf + 1);

                // Match host OS folder based on minimum length to find top level directory to target
                var folder = _folderInfo
                    .Where(x => GetFullName(x).ToLower().Contains(entry.Path.Substring(0, lastIndexOf).ToLower()))
                    .OrderBy(x => GetFullName(x).Length)
                    .FirstOrDefault();

                if (folder == null)
                {
                    Console.WriteLine("Warning: unable to determine target for index entry [{0}]", entry.Path);
                    continue;
                }

                var target = GetFullName(folder) + Separator + filename;
                var sourcePath = _gitRepository.Info.WorkingDirectory + entry.Path;
                indexChanges[sourcePath] = target;
            }

            // Unite the git index with the correct OS folder
            Unite(indexChanges);

            return this;
        }

        /// <summary>
        /// Unite filename casing between git index and host OS
        /// </summary>
        private UniteRepository UniteFilenameCasing()
        {
            if (!_options.HasFlag(OptionFlags.UniteFiles)) return this;

            // The " " at the end of the Path.Combine is a trick to be sure there is a \ at the end of the path
            // Otherwise, we will exclude files such as .gitattributes
            var dotGitFolderPath = Path.Combine(GetFullName(_gitDirectoryInfo), ".git", " ").TrimEnd();
            var files = _gitDirectoryInfo.GetFiles("*", SearchOption.AllDirectories).Where(f => !GetFullName(f).StartsWith(dotGitFolderPath)).ToList();
            var filesFullPathMap = new HashSet<string>(files.ConvertAll(GetFullName));
            var strippedPathMap = new HashSet<string>(filesFullPathMap.Select(x => x.Replace(_gitRepository.Info.WorkingDirectory, string.Empty).ToString()));
            var indexFileEntries = _gitRepository.Index.Select(x => x.Path).Except(strippedPathMap);
            
            foreach (var entry in indexFileEntries)
            {
                var sourcePath = _gitRepository.Info.WorkingDirectory + entry;

                // Match host OS filename based on full pathname ignoring case
                var target = files.FirstOrDefault(f => string.Equals(GetFullName(f), sourcePath, StringComparison.CurrentCultureIgnoreCase));
                if (target == null) continue;

                // Unite the git index with the correct OS folder
                Unite(sourcePath, GetFullName(target));
            }
            return this;
        }

        public void Dispose()
        {
            _gitRepository?.Dispose();
        }
    }
}