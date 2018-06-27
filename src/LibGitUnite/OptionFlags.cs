using System;

namespace LibGitUnite
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
        UniteFiles = 4,
        /// <summary>
        /// Rename files in host OS file system to make them as they are in git index
        /// </summary>
        RenameEntriesInHostOS = 8
    }
}