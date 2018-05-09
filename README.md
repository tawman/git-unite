Git.Unite
=========
Git Unite is a utility that fixes case sensitive file names and paths present in a git repository index on Windows. Since Windows is not case sensitive, the git index case sensitivity issue does not manifest itself until browsing the code repository on GitHub or cloning the repository to a case sensitive file system on Linux.

Introducing case sensitive file paths into the git index on a case insensitive operating system like Windows is easier than you think. A simple `git mv .\Where\Waldo where\is\Waldo` is all you need to create two separate paths in the git index, but the Windows working directory will only report one. There might be git config settings that help avoid this problem, but controlling the settings and behavior of 20+ contributors on a project team is nearly impossible.

The problem is exacerbated when hundreds of files are moved during a repository layout reorganization. If the user moving the files is not careful, these case sensitive path names will pollute the git index but appear fine in the working directory. Cleaning up these case sensitive file path issues on Windows is tedious, and this is where Git Unite helps out.

Git Unite will search the git repository index for file names and paths that do not match the same case that Windows is using. For each git index path case mismatch found, Git Unite will update the git index entry with the case reported by the Windows file system.

    Usage: Git.Unite [OPTIONS]+ repository
    Unite the git repository index file paths with current Windows case usage.
    If no repository path is specified, the current directory is used.
    
    Options:
          --dry-run              dry run without making changes
      -d, --directory-only       only perform directory case changes
      -f, --file-only            only perform filename case changes
      -h, --help                 show this message and exit

Example Usage
---------------- 
    C:\demo [master]> Git.Unite C:\demo
    C:\demo [master +0 ~1 -0]> git status
    # On branch master
    # Changes to be committed:
    #   (use "git reset HEAD <file>..." to unstage)
    #
    #       renamed:    where/is/Waldo -> Where/Is/Waldo
    #

A more detailed example scenario and usage is available on my blog post [Git Unite - Fix Case Sensitive File Paths on Windows](http://www.woodcp.com/2013/01/git-unite-fix-case-sensitive-file-paths-on-windows/ "Wood Consulting Practice, LLC")

How To Build It?
----------------
Open a PowerShell window and run: `build.cmd`

Otherwise, open the Solution file in Visual Studio and Build

The binary will be in `git-unite\src\Git.Unite\bin\Debug`

Thanks
------
This software is open source and check the LICENSE.md file for more details.

Todd A. Wood
([@iToddWood](https://twitter.com/iToddWood "Follow me on Twitter"))
Visit [Implement IToddWood](http://www.woodcp.com "Wood Consulting Practice, LLC")
