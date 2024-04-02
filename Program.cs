using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace RemoveDuplicatedImages
{
    public class Program
    {

        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            IConfiguration config = builder.Build();

            var scriptStartTime = DateTime.Now;
            string rootFolder = config.GetSection("Settings").GetSection("RootFolder").Value ?? throw new InvalidOperationException("Missing RootFolder");
            bool dryRun = Convert.ToBoolean(config.GetSection("Settings").GetSection("DryRun").Value);
            string logPath = config.GetSection("Settings").GetSection("LogFolder").Value;


            DirectoryInfo directoryInfo = new DirectoryInfo(logPath);

            if (!directoryInfo.Exists)
            {
                directoryInfo.Create();
            }

            string fileName = @$"Log_{DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss")}" + ".txt";

            logPath = Path.Combine(logPath, fileName);

            if (!File.Exists(logPath))
            {
                using (var fileStream = File.Create(logPath)) { }
            }

            using (var stream = new FileStream(logPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 256 * 1024))
            {
                using (StreamWriter streamWriter = new StreamWriter(stream))
                {
                    
                    Console.WriteLine($"Application Mode: Dry Run = {dryRun}");
                    Console.WriteLine("Do you want to continue? Y/N");
                    var keyInput = Console.ReadKey();
                    Console.WriteLine();

                    if (keyInput.Key.ToString().ToLower() == "n")
                    {
                        Console.WriteLine("Exiting ...");
                        return;
                    }

                    Console.WriteLine("Scanning for duplicated images ...");


                    streamWriter.WriteLine($"Script started {scriptStartTime}");
                    streamWriter.WriteLine($"Dry run:{dryRun}");
                    RemoveDuplicateFiles(rootFolder, dryRun, streamWriter, -3);

                    streamWriter.WriteLine($"Script done at : {DateTime.Now}");

                    Console.WriteLine("Press any key to Exit");
                    Console.ReadKey();

                }
            };
        }


        static void RemoveDuplicateFiles(string foldername, bool dryRun, StreamWriter stream, int level)
        {
            var hashCollection = new HashSet<string>();

            if (level == 1)
            {

                foreach (var file in Directory.EnumerateFiles(foldername, "*.jpg"))
                {
                    

                    if (!hashCollection.Add(CalculateMD5(file)))
                    {
                        if (!dryRun)
                        {
                            File.Delete(file);
                            stream.WriteLine($"Removed duplicate. File: {file}");
                        }
                        else
                        {
                            stream.WriteLine($"NOT REMOVED. Settings is DRY RUN.Duplicate : {file}");
                        }

                        Console.WriteLine($"Duplicate : {file} Level: {level}");
                    }


                }

             
            }

            foreach (var directory in Directory.EnumerateDirectories(foldername))
            {
                RemoveDuplicateFiles(directory, dryRun, stream, level +1);
            }

        
        }




        static string CalculateMD5(string filename)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filename);
            var hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }


    }
}

