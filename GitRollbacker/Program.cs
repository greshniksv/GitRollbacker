using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GitRollbacker.Models;
using LibGit2Sharp;
using Newtonsoft.Json;

namespace GitRollbacker
{
    class Program
    {
        static void Main(string[] args)
        {
            Config config;
            const string ConfigFile = "GitRollbacker.json";

            if (!File.Exists(ConfigFile))
            {
                config = new Config
                {
                    Location = @"c:\project",
                    IgnoreItems = new[] { "test.dll" },
                    RollbackItems = new[] { ".*dll" }
                };

                var configData = JsonConvert.SerializeObject(config, Formatting.Indented);
                using (TextWriter textWriter = new StreamWriter(ConfigFile))
                {
                    textWriter.Write(configData);
                }

                return;
            }

            config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(ConfigFile));
            if (string.IsNullOrEmpty(config.Location))
            {
                return;
            }

            using (var repo = new Repository(config.Location))
            {
                Console.WriteLine(" > Add to revert collection");
                var listToRevert = new List<string>();
                foreach (TreeEntryChanges changes in repo.Diff.Compare<TreeChanges>())
                {
                    foreach (var configRollbackItem in config.RollbackItems)
                    {
                        Regex regex = new Regex(configRollbackItem);
                        if (regex.IsMatch(changes.Path))
                        {
                            if (!listToRevert.Contains(changes.Path))
                            {
                                Console.WriteLine(changes.Path);
                                listToRevert.Add(changes.Path);
                            }
                        }
                    }
                }

                Console.WriteLine("\n\n > Apply ignore rules");
                for (int i = 0; i < listToRevert.Count; i++)
                {
                    foreach (var ignoreItem in config.IgnoreItems)
                    {
                        Regex regex = new Regex(ignoreItem);
                        if (regex.IsMatch(listToRevert[i]))
                        {
                            Console.WriteLine(listToRevert[i]);
                            listToRevert[i] = string.Empty;
                        }
                    }
                }

                listToRevert.RemoveAll(string.IsNullOrEmpty);

                Console.WriteLine("\n\n > Reverting");
                foreach (var item in listToRevert)
                {
                    try
                    {
                        repo.CheckoutPaths("HEAD", new[] { item },
                            new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("\n > ------------- REVERT ERROR -----------------");
                        Console.WriteLine("Path: {0}", item);
                        Console.WriteLine(ex.Message);
                    }
                }
            }

            Console.WriteLine("\n\n\nAll done!");
            Console.ReadKey();
        }
    }
}
