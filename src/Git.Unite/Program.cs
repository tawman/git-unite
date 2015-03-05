﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NDesk.Options;
using LibGitUnite;

namespace Git.Unite
{
    static class Program
    {
        static void Main(string[] args)
        {
            var options = GitUnite.OptionFlags.UniteDirectories | GitUnite.OptionFlags.UniteFiles;
            var showHelp = false;
            List<string> paths;
            var opts = new OptionSet
                {
                    {"dry-run", "dry run without making changes",
                        v => options  |= v != null ? GitUnite.OptionFlags.DryRun : options},
                    {"d|directory-only", "only perform directory case changes",
                        v => { if (v != null) options = (options & ~GitUnite.OptionFlags.UniteFiles      ) | GitUnite.OptionFlags.UniteDirectories;}},
                    {"f|file-only", "only perform filename case changes",
                        v => { if (v != null) options = (options & ~GitUnite.OptionFlags.UniteDirectories) | GitUnite.OptionFlags.UniteFiles      ;}},
                    {"r|rename-local", "rename local files/directories to match the repo",
                        v => options  |= v != null ? GitUnite.OptionFlags.RenameLocal : options}, 
                    {"h|help", "show this message and exit",
                        v => showHelp  = v != null}
                };

            try
            {
                paths = opts.Parse(args);
            }
            catch (OptionException ex)
            {
                Console.Write("Git.Unite: ");
                Console.WriteLine(ex.Message);
                Console.WriteLine("Try `Git.Unite --help' for more information.");
                return;
            }

            if (showHelp)
            {
                ShowHelp(opts);
                return;
            }

            if(!paths.Any())
                paths.Add(Directory.GetCurrentDirectory());
            
            foreach(var path in paths)
            {
                if(!Directory.Exists(path+"\\.git"))
                {					
                    Console.WriteLine(path+" does not appear to be a valid git repository");
                    return;
                }
                
                GitUnite.Process(path, options);
            }
        }

        static void ShowHelp(OptionSet opts)
        {
            Console.WriteLine("Usage: Git.Unite [OPTIONS]+ repository");
            Console.WriteLine("Unite the git repository index file paths with current Windows case usage.");
            Console.WriteLine("If no repository path is specified, the current directory is used.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            opts.WriteOptionDescriptions(Console.Out);
        }
    }
}
