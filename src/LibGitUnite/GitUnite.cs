using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LibGitUnite
{
    public static class GitUnite
    {
        private const string Separator = "\\";

        /// <summary>
        /// Unite the git repository index file paths with the same case the OS is using
        /// </summary>
        /// <param name = "path">
        ///   The path to the git repository to open, can be either the path to the git directory (for non-bare repositories this
        ///   would be the ".git" folder inside the working directory) or the path to the working directory.
        /// </param>
        /// <param name="dryrun">dry run without making changes</param>
        public static void Process(string path, bool dryrun)
        {
            // Build a list of directory names as seen by the host operating system
            List<string> folders;

            try
            {
                folders = Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories).ToList();
            }
            catch (Exception ex)
            {
                Console.Write("Git.Unite: ");
                Console.WriteLine(ex.Message);
                return;
            }

            if (!folders.Any()) return;

            using (var repo = new UniteRepository(path))
            {
                // Find all repository files with directory paths not found in the host OS folder collection
                var indexEntries =
                    repo.Index.Where(f => f.Path.LastIndexOf(Separator, StringComparison.Ordinal) != -1
                                          &&
                                          !folders.Any(s => s.Contains(f.Path.Substring(0, f.Path.LastIndexOf(Separator, StringComparison.Ordinal)))));

                // Unite the casing of the repository file directory path with the casing seen by the host operating system
                foreach (var entry in indexEntries)
                {
                    var lastIndexOf = entry.Path.LastIndexOf(Separator, StringComparison.Ordinal);
                    var filename = entry.Path.Substring(lastIndexOf + 1);

                    // Match host OS folder based on minimum length to find top level directory to target
                    var target = folders
                                     .Where(x => x.ToLower().Contains(entry.Path.Substring(0, lastIndexOf).ToLower()))
                                     .OrderBy(x => x.Length)
                                     .FirstOrDefault() + Separator + filename;

                    var sourcePath = repo.Info.WorkingDirectory + entry.Path;

                    // Unite the git index with the correct OS folder
                    if(dryrun)
                        Console.WriteLine("proposed change: {0} -> {1}", sourcePath, target);
                    else
                        repo.Unite(sourcePath, target);
                }
            }
        }
    }
}