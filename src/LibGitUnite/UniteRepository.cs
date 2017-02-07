using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        /// <summary>
        /// Perform a dry run (--dry-run) only and report proposed changes
        /// </summary>
        private bool DryRunOnly => _options.HasFlag(OptionFlags.DryRun);

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
            _gitRepository = new Repository(_gitDirectoryInfo.FullName);
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
            if (DryRunOnly)
            {
                Console.WriteLine("proposed rename: {0} -> {1}", sourcePath, destinationPath);
                return;
            }

            try
            {
                _gitRepository.Index.Remove(sourcePath.Replace(_gitRepository.Info.WorkingDirectory, string.Empty));
                _gitRepository.Index.Add(destinationPath.Replace(_gitRepository.Info.WorkingDirectory, string.Empty));
                _gitRepository.Index.Write();
            }
            catch (Exception ex)
            {
                Console.WriteLine("error changing: {0} -> {1} [{2}]", sourcePath, destinationPath, ex.Message);
            }
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
                .Where(d => !d.FullName.ToLowerInvariant().StartsWith(_gitRepository.Info.Path.TrimEnd('\\').ToLowerInvariant()))
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

            var foldersFullPathMap = new HashSet<string>(_folderInfo.ConvertAll(s => s.FullName));

            // Find all repository files with directory paths not found in the host OS folder collection
            var indexEntries =
                _gitRepository.Index.Where(f => f.Path.LastIndexOf(Separator, StringComparison.Ordinal) != -1
                                      &&
                                      !foldersFullPathMap.Any(s => s.Contains(f.Path.Substring(0, f.Path.LastIndexOf(Separator, StringComparison.Ordinal)))));

            // Unite the casing of the repository file directory path with the casing seen by the host operating system
            foreach (var entry in indexEntries)
            {
                var lastIndexOf = entry.Path.LastIndexOf(Separator, StringComparison.Ordinal);
                var filename = entry.Path.Substring(lastIndexOf + 1);

                // Match host OS folder based on minimum length to find top level directory to target
                var folder = _folderInfo
                    .Where(x => x.FullName.ToLower().Contains(entry.Path.Substring(0, lastIndexOf).ToLower()))
                    .OrderBy(x => x.FullName.Length)
                    .FirstOrDefault();

                if (folder == null)
                {
                    Console.WriteLine("Warning: unable to determine target for index entry [{0}]", entry.Path);
                    continue;
                }

                var target = folder.FullName + Separator + filename;
                var sourcePath = _gitRepository.Info.WorkingDirectory + entry.Path;

                // Unite the git index with the correct OS folder
                Unite(sourcePath, target);
            }
            return this;
        }

        /// <summary>
        /// Unite filename casing between git index and host OS
        /// </summary>
        private UniteRepository UniteFilenameCasing()
        {
            if (!_options.HasFlag(OptionFlags.UniteFiles)) return this;

            var files = GetAllFileInfos();
            var filesFullPathMap = new HashSet<string>(files.ConvertAll(s => s.FullName));
            var indexFileEntries = _gitRepository.Index.Where(f => filesFullPathMap.All(s => s.Replace(_gitRepository.Info.WorkingDirectory, string.Empty) != f.Path));

            foreach (var entry in indexFileEntries)
            {
                var sourcePath = _gitRepository.Info.WorkingDirectory + entry.Path;

                // Match host OS filename based on full pathname ignoring case
                var target = files.FirstOrDefault(f => string.Equals(f.FullName, sourcePath, StringComparison.CurrentCultureIgnoreCase));
                if (target == null) continue;

                // Unite the git index with the correct OS folder
                Unite(sourcePath, target.FullName);
            }
            return this;
        }

        /// <summary>
        /// Get a list of files in all host operating system folders identified
        /// </summary>
        /// <returns>A list of <see cref="FileInfo"/> reported by host OS</returns>
        /// ReSharper disable once ParameterTypeCanBeEnumerable.Local
        private List<FileInfo> GetAllFileInfos()
        {
            var fileInfo = new List<FileInfo>();

            try
            {
                fileInfo = _folderInfo
                    .Union(new[] { _gitDirectoryInfo })
                    .SelectMany(f => f.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.Write("Git.Unite: ");
                Console.WriteLine(ex.Message);
            }

            return fileInfo;
        }

        public void Dispose()
        {
            _gitRepository?.Dispose();
        }
    }
}