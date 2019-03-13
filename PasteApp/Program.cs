using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace PasteApp
{
    class Program
    {
        /// <summary>
        /// Extracts paths of folders and files which are to be copied.
        /// /// </summary>
        /// <returns>
        /// First string is the path of the root folder of folders and files to be copied.
        /// Remaining strings represent local paths of folders and files. 
        /// </returns>
        private static string[] GetFoldersAndFilesToCopy()
        {
            string inputFile = @"C:\Users\toshiba\Documents\MultithreadWindowsCopy\filesToCopy.out";
            return File.ReadAllLines(inputFile);
        }

        /// <summary>
        /// Calculates the size of a directory. 
        /// </summary>
        /// <param name="dirInfo">DirectoryInfo instance.</param>
        /// <returns>Directory size in bytes.</returns>
        private static long GetDirectorySize(DirectoryInfo dirInfo)
        {
            long sizeInBytes = 0;
            sizeInBytes += dirInfo.EnumerateFiles().Sum(fi => fi.Length);
            sizeInBytes += dirInfo.EnumerateDirectories().Sum(di => GetDirectorySize(di));
            return sizeInBytes;
        }

        /// <summary>
        /// Returns the total file size in all directories and subdirectories.
        /// </summary>
        /// <param name="root">Absoulte path of root folder of items to be copied.</param>
        /// <param name="itemsToCopy">Names of folders and files to be copied.</param>
        /// <returns>Total file size, including files in subdirectories.</returns>
        private static long GetTotalFileSize(string root, string[] itemsToCopy)
        {
            long sizeInBytes = 0;
            foreach(string item in itemsToCopy)
            {
                string path = Path.Combine(root, item);
                if (File.Exists(path) == true)
                    sizeInBytes += (new FileInfo(path)).Length;
                else if (Directory.Exists(path) == true)
                    sizeInBytes += GetDirectorySize(new DirectoryInfo(path));
                else
                    throw new Exception("Given path is not a file nor a directory: " + path + "!");
            }

            return sizeInBytes;
        }

        /// <summary>
        /// Returns the total file count in all directories and subdirectories.
        /// </summary>
        /// <param name="root">Absoulte path of root folder of items to be copied.</param>
        /// <param name="itemsToCopy">Names of folders and files to be copied.</param>
        /// <returns>Total file count, including files in subdirectories.</returns>
        private static long GetTotalFileCount(string root, string[] itemsToCopy)
        {
            long count = 0;
            foreach(string item in itemsToCopy)
            {
                string path = Path.Combine(root, item);
                if (File.Exists(path) == true)
                    count++;
                else if (Directory.Exists(path) == true)
                    count += Directory.GetFiles(path, "*", SearchOption.AllDirectories).LongLength;
                else
                    throw new Exception("Given path is not a file nor a directory: " + path + "!");
            }

            return count;
        }

        /// <summary>
        /// Encloses path in double quotes in case the path contains blanks.
        /// </summary>
        /// <param name="path">Path to enclose.</param>
        /// <returns>Path enclosed in double quotes.</returns>
        private static string QuoteEnclose(string path)
        {
            return '"' + path + '"';
        }

        /// <summary>
        /// Formatted current time.
        /// </summary>
        /// <returns>String representing date and tim in format "hh:mm:ss.ffffff ".</returns>
        public static string CurrentTime()
        {
            return DateTime.Now.ToString("hh:mm:ss.ffffff ");
        }

        private static readonly string productName = "MultithreadWindowsCopy";

        static void Main(string[] args)
        {             
            // Used for debbuging.
            Debug.Listeners.Add(new TextWriterTraceListener(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), productName, "debug.log")));
            Debug.AutoFlush = true;

            Debug.WriteLine(Environment.NewLine + CurrentTime() + "Paste started");

            // Destination folder.
            string destination;
            try
            {
                destination = args[0];
                Debug.WriteLine(CurrentTime() + "Paste path: " + args[0]);
            }
            catch(Exception e)
            {
                Debug.WriteLine(CurrentTime() + "[ERROR]" + e.Message);
                Debug.WriteLine(e.StackTrace);
                return ;
            }

            List<string> fileContents = GetFoldersAndFilesToCopy().ToList<string>();
            string root = fileContents[0];
            fileContents.RemoveAt(0);
            string[] foldersAndFiles = fileContents.ToArray<string>();

            Debug.WriteLine(CurrentTime() + "Items scheduled for pasting:");
            Debug.WriteLine(root + @"\");
            Debug.Indent();
            foreach (string item in foldersAndFiles)
                Debug.WriteLine(item);
            Debug.Unindent();

            Debug.WriteLine(CurrentTime() + "Total file count: " + GetTotalFileCount(root, foldersAndFiles));
            Debug.WriteLine(CurrentTime() + "Total file size: " + GetTotalFileSize(root, foldersAndFiles));

            // Create robocopy script.
            StringBuilder sb = new StringBuilder();
            foreach (string item in foldersAndFiles)
            {
                string itemPath = Path.Combine(root, item);
                if (File.Exists(itemPath) == true)
                    sb.AppendLine("robocopy " + QuoteEnclose(root) + " " + QuoteEnclose(destination) + " " + QuoteEnclose(item));
                else if (Directory.Exists(itemPath) == true)
                    sb.AppendLine("robocopy " + QuoteEnclose(itemPath) + " " + QuoteEnclose(Path.Combine(destination, item)) + " /e");
                else
                    throw new Exception("Given path is not a file nor a directory: " + itemPath + "!");
            }
            string copyScript = sb.ToString();
            Debug.WriteLine(Environment.NewLine + CurrentTime() + "Script to execute:");
            Debug.WriteLine(copyScript);
         
            // Run robocopy script
            using (Process p = new Process())
            {
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;

                Debug.WriteLine(CurrentTime() + "Executing script...");
                p.Start();

                StreamWriter sw = p.StandardInput;
                foreach (string line in copyScript.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
                {
                    if (sw.BaseStream.CanWrite == true)
                        sw.WriteLine(line);
                    else
                        throw new Exception("Cannot execute robocopy command, base stream doesn't allow writes!");
                    
                }
            }
            Debug.WriteLine(CurrentTime() + "Done");

            Debug.WriteLine(CurrentTime() + "PasteApp finished.");
        }
    }
}
