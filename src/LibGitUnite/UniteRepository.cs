using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using LibGit2Sharp;
using System.IO;

namespace LibGitUnite
{
    public class UniteRepository : Repository
    {
        private readonly MethodInfo _prepareBatch;
        private readonly MethodInfo _removeFromIndex;
        private readonly MethodInfo _addToIndex;
        private readonly MethodInfo _updatePhysicalIndex;
        private readonly GitUnite.OptionFlags _options;

        /// <summary>
        /// Perform a dry run (--dry-run) only and report proposed changes
        /// </summary>
        private bool DryRunOnly
        {
            get { return _options.HasFlag(GitUnite.OptionFlags.DryRun); }
        }
        
        private bool RenameLocal
        {
            get { return _options.HasFlag(GitUnite.OptionFlags.RenameLocal); }
        }

        /// <summary>
        /// Extended LibGit2Sharp.Repository with the Unite version of the Move command
        /// </summary>
        /// <param name = "path">
        ///   The path to the git repository to open, can be either the path to the git directory (for non-bare repositories this
        ///   would be the ".git" folder inside the working directory) or the path to the working directory.
        /// </param>
        /// <param name="options">Runtime command line options specified</param>
        public UniteRepository(string path, GitUnite.OptionFlags options)
            : base(path, null)
        {
            _options = options;

            _prepareBatch = Index.GetType().GetMethod(
                "PrepareBatch",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(IEnumerable<string>), typeof(IEnumerable<string>) },
                null);

            _removeFromIndex = Index.GetType().GetMethod("RemoveFromIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            _addToIndex = Index.GetType().GetMethod("AddToIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            _updatePhysicalIndex = Index.GetType().GetMethod("UpdatePhysicalIndex", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        /// <summary>
        /// Modified version of LibGit2Sharp.Index.Move without the OS file system checks to unite file with proper case path
        /// </summary>
        /// <param name = "sourcePath">The path of the file within the working directory which has to be renamed.</param>
        /// <param name = "destinationPath">The target path of the file within the working directory.</param>
        public void Unite(string sourcePath, string destinationPath)
        {
            Unite(new[] { sourcePath }, new[] { destinationPath });
        }

        /// <summary>
        /// Modified version of LibGit2Sharp.Index.Move without the OS file system checks to unite file with proper case path
        /// </summary>
        /// <param name="sourcePaths">List containing path of each file within the working directory which has to be renamed.</param>
        /// <param name="destinationPaths">List containing target path of each file within the working directory.</param>
        /// ReSharper disable once MemberCanBePrivate.Global
        public void Unite(IEnumerable<string> sourcePaths, IEnumerable<string> destinationPaths)
        {
            if (sourcePaths == null)
                throw new ArgumentNullException("sourcePaths");

            if (destinationPaths == null)
                throw new ArgumentNullException("destinationPaths");

            var lst = sourcePaths.Zip(destinationPaths
                                     ,(a, b) => new { Src = RenameLocal ? b : a
                                                    , Dst = RenameLocal ? a : b });
            
            if (DryRunOnly) {
                foreach (var kv in lst)
                    Console.WriteLine("proposed rename: {0} -> {1}", kv.Src, kv.Dst);
                
                return;
            }

            if (RenameLocal) {
                Bullshit_StringString renameTemp = delegate(String a) {
                    for (int i = 0;; ++i) {
                        var pathTmp = a + i;
                        
                        try {
                            File.Move(a, pathTmp);
                            return pathTmp;
                        } catch (IOException e) {
                            if (!File.Exists(a)) throw e; // If it failed b/c 404 then don't catch
                            // Ignore dest-already-exists, however
                        }
                    }
                };
                
                foreach (var kv in lst) {
                    Console.WriteLine("renaming local: {0} -> {1}", kv.Src, kv.Dst);
                    var p = renameTemp(kv.Src);
                    File.Move(p, kv.Dst);
                }
            } else {
                dynamic batch = _prepareBatch.Invoke(Index, new object[] { sourcePaths, destinationPaths });

                if (batch.Count == 0)
                    throw new ArgumentNullException("sourcePaths");

                foreach (var keyValuePair in batch)
                {
                    var from = keyValuePair.Key.Item1;
                    var to = keyValuePair.Value.Item1;
                    try
                    {
                        _removeFromIndex.Invoke(Index, new object[] { from });
                        _addToIndex.Invoke(Index, new object[] { to });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("error changing: {0} -> {1} [{2}]", from, to, ex.Message);
                    }

                    _updatePhysicalIndex.Invoke(Index, new object[] { });
                }
            }
        }
        private delegate String Bullshit_StringString(String a);
    }
}