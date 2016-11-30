using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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

        private static FieldInfo FullNameField = typeof(FileSystemInfo).GetField(
                            "FullPath",
                             BindingFlags.Instance |
                             BindingFlags.NonPublic);

        private static string GetFullName(FileSystemInfo fsi)
        {
            return (string)FullNameField.GetValue(fsi);
        }

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

            using (var repo = new UniteRepository(GetFullName(gitPathInfo), options))
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

            var foldersFullPathMap = new HashSet<String>(folders.ConvertAll(s => GetFullName(s)));

            // Find all repository files with directory paths not found in the host OS folder collection
            var indexEntries =
                repo.Index.Where(f => f.Path.LastIndexOf(Separator, StringComparison.Ordinal) != -1
                                      &&
                                      !foldersFullPathMap.Any(s => s.Contains(f.Path.Substring(0, f.Path.LastIndexOf(Separator, StringComparison.Ordinal)))));

            // Unite the casing of the repository file directory path with the casing seen by the host operating system
            foreach (var entry in indexEntries)
            {
                var lastIndexOf = entry.Path.LastIndexOf(Separator, StringComparison.Ordinal);
                var filename = entry.Path.Substring(lastIndexOf + 1);

                // Match host OS folder based on minimum length to find top level directory to target
                var folder = folders
                    .Where(x => GetFullName(x).ToLower().Contains(entry.Path.Substring(0, lastIndexOf).ToLower()))
                    .OrderBy(x => GetFullName(x).Length)
                    .FirstOrDefault();

                if (folder == null)
                {
                    Console.WriteLine("Warning: unable to determine target for index entry [{0}]", entry.Path);
                    continue;
                };

                var target = GetFullName(folder) + Separator + filename;
                var sourcePath = repo.Info.WorkingDirectory + entry.Path;

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
            // The " " at the end of the Path.Combine is a trick to be sure there is a \ at the end of the path
            // Otherwise, we will exclude files suche as .gitattributes
            var dotGitFolderPath = Path.Combine(GetFullName(gitPathInfo), ".git", " ").TrimEnd();
            var files = gitPathInfo.GetFiles("*", SearchOption.AllDirectories).Where(f => !GetFullName(f).StartsWith(dotGitFolderPath)).ToList();
            var filesFullPathMap = new HashSet<String>(files.ConvertAll(s => GetFullName(s)));


            var indexFileEntries = repo.Index.Where(f => filesFullPathMap.All(s => s.Replace(repo.Info.WorkingDirectory, string.Empty) != f.Path));

            foreach (var entry in indexFileEntries)
            {
                var sourcePath = repo.Info.WorkingDirectory + entry.Path;

                // Match host OS filename based on full pathname ignoring case
                var target = files.FirstOrDefault(f => String.Equals(GetFullName(f), sourcePath, StringComparison.CurrentCultureIgnoreCase));
                if (target == null) continue;

                // Unite the git index with the correct OS folder
                repo.Unite(sourcePath, GetFullName(target));
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
                .Where(d => !GetFullName(d).ToLowerInvariant().StartsWith(repo.Info.Path.TrimEnd('\\').ToLowerInvariant()))
                .ToList();
            }
            catch (Exception ex)
            {
                Console.Write("Git.Unite: ");
                Console.WriteLine(ex.Message);
            }

            return folderInfo;
        }
    }
}