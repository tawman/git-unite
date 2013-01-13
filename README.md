Git.Unite
=========
Git Unite is a utility that fixes case sensitive file paths present in a git repository index on Windows. Since Windows is not case sensitive, the git index case sensitivity issue does not manifest itself until browsing the code repository on GitHub or cloning the repository to a case sensitive operating system like Linux.

Introducing case sensitive file paths into the git index on a case insensitive operating system like Windows is easier than you think. A simple '*git mv .\Where\Waldo where\is\Waldo*' is all you need to create two separate paths into the git index while the Windows working directory will only report one.

The problem is exacerbated when hundreds of files are moved during a repository layout reorganization. If the user moving the files is not careful, these case sensitive path names will populate the git index but appear fine in the working directory. Cleaning these case sensitive file path issues on Windows is tedious and this is where Git Unite helps out.

Git Unite will search the git repository index for file paths that do not match the current working directory file path casing. For each git index path case mismatch found, Git Unite will update the git index entry with the same case as seen by Windows.

    Usage: Git.Unite [OPTIONS]+ repository
    Unite the git repository index file paths with current Windows case usage.
    If no repository path is specified, the current directory is used.
    
    Options:
          --dry-run              dry run without making changes
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
How To Build It?
----------------
From a command window run: *build.bat*

Otherwise, open the Solution file in Visual Studio 2010 and Build

Thanks
------
This software is open source and check the LICENSE.md file for more details.

Todd A. Wood
([@iToddWood](https://twitter.com/iToddWood "Follow me on Twitter"))
Visit [Implement IToddWood](http://www.woodcp.com "Wood Consulting Practice, LLC")
