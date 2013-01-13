using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NDesk.Options;
using LibGitUnite;

namespace Git.Unite
{
    class Program
    {
        static void Main(string[] args)
        {
            var dryrun = false;
            var showHelp = false;
            List<string> paths;
            var opts = new OptionSet
                {
                    {"dry-run", "dry run without making changes", v => dryrun = v != null},
                    {"h|help", "show this message and exit", v => showHelp = v != null}
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

            paths.ForEach(p => GitUnite.Process(p, dryrun));
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
