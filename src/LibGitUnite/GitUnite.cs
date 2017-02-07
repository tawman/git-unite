using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LibGitUnite
{
    public static class GitUnite
    {
        /// <summary>
        /// Git.Unite Runtime Option Flags
        /// </summary>
        [Flags]
        public enum OptionFlags : short
        {
            /// <summary>
            /// Perform a dry run (--dry-run) only and report proposed changes
            /// </summary>
            DryRun = 1,
            /// <summary>
            /// Process directory names for case changes
            /// </summary>
            UniteDirectories = 2,
            /// <summary>
            /// Process filenames for case changes
            /// </summary>
            UniteFiles = 4
        }

        private const string Separator = "\\";

        /// <summary>
        /// Unite the git repository index file paths with the same case the OS is using
        /// </summary>
        /// <param name = "gitPath">
        ///   The path to the git repository to open, can be either the path to the git directory (for non-bare repositories this
        ///   would be the ".git" folder inside the working directory) or the path to the working directory.
        /// </param>
        /// <param name="options">Runtime command line options specified</param>
        public static void Process(string gitPath, OptionFlags options)
        {
            var gitPathInfo = new DirectoryInfo(gitPath);

            using (var repo = new UniteRepository(gitPathInfo.FullName, options))
            {
                // Build a list of directory names as seen by the host operating system
                var folders = repo.GetHostDirectoryInfo(gitPathInfo);

                if(options.HasFlag(OptionFlags.UniteDirectories))
                    repo.UniteFolderCasing(folders);

                if (options.HasFlag(OptionFlags.UniteFiles))
                    repo.UniteFilenameCasing(gitPathInfo, folders);
            }
        }

        /// <summary>
        /// Unite directory name casing between git index and host OS
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="folders"></param>
        private static void UniteFolderCasing(this UniteRepository repo, List<DirectoryInfo> folders)
        {
            if (!folders.Any()) return;

            var foldersFullPathMap = new HashSet<String>(folders.ConvertAll(s => s.FullName));

            // Find all repository files with directory paths not found in the host OS folder collection
            var indexEntries =
                repo.GitRepository.Index.Where(f => f.Path.LastIndexOf(Separator, StringComparison.Ordinal) != -1
                                      &&
                                      !foldersFullPathMap.Any(s => s.Contains(f.Path.Substring(0, f.Path.LastIndexOf(Separator, StringComparison.Ordinal)))));

            // Unite the casing of the repository file directory path with the casing seen by the host operating system
            foreach (var entry in indexEntries)
            {
                var lastIndexOf = entry.Path.LastIndexOf(Separator, StringComparison.Ordinal);
                var filename = entry.Path.Substring(lastIndexOf + 1);

                // Match host OS folder based on minimum length to find top level directory to target
                var folder = folders
                    .Where(x => x.FullName.ToLower().Contains(entry.Path.Substring(0, lastIndexOf).ToLower()))
                    .OrderBy(x => x.FullName.Length)
                    .FirstOrDefault();

                if (folder == null)
                {
                    Console.WriteLine("Warning: unable to determine target for index entry [{0}]", entry.Path);
                    continue;
                };

                var target = folder.FullName + Separator + filename;
                var sourcePath = repo.GitRepository.Info.WorkingDirectory + entry.Path;

                // Unite the git index with the correct OS folder
                repo.Unite(sourcePath, target);
            }
        }

        /// <summary>
        /// Unite filename casing between git index and host OS
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="gitPathInfo"></param>
        /// <param name="folders"></param>
        private static void UniteFilenameCasing(this UniteRepository repo, DirectoryInfo gitPathInfo, List<DirectoryInfo> folders)
        {
            var files = folders.GetAllFileInfos(gitPathInfo);
            var filesFullPathMap = new HashSet<String>(files.ConvertAll(s => s.FullName));
            var indexFileEntries = repo.GitRepository.Index.Where(f => filesFullPathMap.All(s => s.Replace(repo.GitRepository.Info.WorkingDirectory, string.Empty) != f.Path));

            foreach (var entry in indexFileEntries)
            {
                var sourcePath = repo.GitRepository.Info.WorkingDirectory + entry.Path;

                // Match host OS filename based on full pathname ignoring case
                var target = files.FirstOrDefault(f => String.Equals(f.FullName, sourcePath, StringComparison.CurrentCultureIgnoreCase));
                if (target == null) continue;

                // Unite the git index with the correct OS folder
                repo.Unite(sourcePath, target.FullName);
            }
        }

        /// <summary>
        /// Get a list of directory names as seen by the host operating system
        /// </summary>
        /// <param name="repo">The <see cref="UniteRepository"/></param>
        /// <param name="path">Git path <see cref="DirectoryInfo"/></param>
        /// <returns>A list of <see cref="DirectoryInfo"/> reported by host OS</returns>
        private static List<DirectoryInfo> GetHostDirectoryInfo(this UniteRepository repo, DirectoryInfo path)
        {
            var folderInfo = new List<DirectoryInfo>();

            try
            {
                folderInfo = path.EnumerateDirectories("*", SearchOption.AllDirectories)
                .Where(d => !d.FullName.ToLowerInvariant().StartsWith(repo.GitRepository.Info.Path.TrimEnd('\\').ToLowerInvariant()))
                .ToList();
            }
            catch (Exception ex)
            {
                Console.Write("Git.Unite: ");
                Console.WriteLine(ex.Message);
            }

            return folderInfo;
        }

        /// <summary>
        /// Get a list of files in all host operating system folders identified
        /// </summary>
        /// <param name="folders">A list of <see cref="DirectoryInfo"/> reported by host OS</param>
        /// <param name="rootFolder"></param>
        /// <returns>A list of <see cref="FileInfo"/> reported by host OS</returns>
        /// ReSharper disable once ParameterTypeCanBeEnumerable.Local
        private static List<FileInfo> GetAllFileInfos(this List<DirectoryInfo> folders, DirectoryInfo rootFolder)
        {
            var fileInfo = new List<FileInfo>();

            try
            {
                fileInfo = folders
                    .Union(new [] { rootFolder })
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
    }
}