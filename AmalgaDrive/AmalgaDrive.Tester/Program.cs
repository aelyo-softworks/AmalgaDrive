using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using AmalgaDrive.Configuration;
using AmalgaDrive.Model;
using ShellBoost.Core.Utilities;

namespace AmalgaDrive.Tester
{
    class Program
    {
        private static readonly Random _rnd = new Random(Environment.TickCount);

        static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;

            // get the first localhost drive
            var settings = Settings.Current.DriveServiceSettings.Where(d => d.BaseUrl.IndexOf("//localhost") > 0).FirstOrDefault();
            if (settings == null)
            {
                Console.WriteLine("Did not found a localhost drive.");
                return;
            }

            Console.WriteLine("Found '" + settings.Name + "' drive.");

            using (var service = new DriveService(settings))
            {
                Console.CancelKeyPress += (s, e) =>
                {
                    Console.ResetColor(); // logger uses colors
                    service.Dispose();
                    Console.WriteLine("Aborted.");
                };

                service.OnDemandSynchronizer.Logger = new Logger();
                Console.WriteLine(" Root Path: " + service.RootPath);
                Console.WriteLine(" Sync Period: " + service.OnDemandSynchronizer.SyncPeriod);

                var rootPath = service.OnDemandSynchronizer.RootPath;
                var waitTime = CommandLine.GetArgument("wait", 500);

                Console.WriteLine();
                Console.WriteLine("Press ESC to quit.");
                Console.WriteLine("Press C to clear.");
                Console.WriteLine("Press N to create files.");
                Console.WriteLine("Press D to delete files.");
                Console.WriteLine("Press R to rename files.");

                do
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape)
                        break;

                    switch (key.Key)
                    {
                        case ConsoleKey.S:
                            service.OnDemandSynchronizer.Synchronize();
                            break;

                        case ConsoleKey.C:
                            Console.Clear();
                            break;

                        case ConsoleKey.N:
                            int max = 10;
                            Console.WriteLine(">>> Creating " + max + " test file(s) under " + rootPath);
                            for (int i = 0; i < max; i++)
                            {
                                var path = Path.Combine(rootPath, "test." + i + ".auto.txt");
                                File.WriteAllText(path, "this is test #" + i + " for path " + path);
                                Thread.Sleep(_rnd.Next(waitTime));
                            }
                            break;

                        case ConsoleKey.D:
                            Console.WriteLine(">>> Deleting all test files under " + rootPath);
                            foreach (var file in Directory.GetFiles(rootPath, "*.auto.txt"))
                            {
                                IOUtilities.FileDelete(file);
                                Thread.Sleep(_rnd.Next(waitTime));
                            }
                            break;

                        case ConsoleKey.R:
                            Console.WriteLine(">>> Renaming all test files under " + rootPath);
                            foreach (var file in Directory.GetFiles(rootPath, "*.auto.txt"))
                            {
                                var newPath = Path.Combine(Path.GetDirectoryName(file), "_" + Path.GetFileName(file));
                                IOUtilities.FileMove(file, newPath);
                                Thread.Sleep(_rnd.Next(waitTime));
                            }
                            break;
                    }
                }
                while (true);
                Console.ResetColor();
            }
        }
    }
}
