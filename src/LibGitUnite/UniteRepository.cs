using System;
using LibGit2Sharp;

namespace LibGitUnite
{
    public class UniteRepository : IDisposable
    {
        private readonly GitUnite.OptionFlags _options;
        public Repository GitRepository { get; }

        /// <summary>
        /// Perform a dry run (--dry-run) only and report proposed changes
        /// </summary>
        private bool DryRunOnly => _options.HasFlag(GitUnite.OptionFlags.DryRun);

        /// <summary>
        /// Extended LibGit2Sharp.Repository with the Unite version of the Move command
        /// </summary>
        /// <param name = "path">
        ///   The path to the git repository to open, can be either the path to the git directory (for non-bare repositories this
        ///   would be the ".git" folder inside the working directory) or the path to the working directory.
        /// </param>
        /// <param name="options">Runtime command line options specified</param>
        public UniteRepository(string path, GitUnite.OptionFlags options)
        {
            GitRepository = new Repository(path);
            _options = options;
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
                GitRepository.Index.Remove(sourcePath.Replace(GitRepository.Info.WorkingDirectory, string.Empty));
                GitRepository.Index.Add(destinationPath.Replace(GitRepository.Info.WorkingDirectory, string.Empty));
                GitRepository.Index.Write();
            }
            catch (Exception ex)
            {
                Console.WriteLine("error changing: {0} -> {1} [{2}]", sourcePath, destinationPath, ex.Message);
            }
        }

        public void Dispose()
        {
            GitRepository?.Dispose();
        }
    }
}