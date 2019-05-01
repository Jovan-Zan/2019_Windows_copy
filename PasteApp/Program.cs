using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

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
            return DateTime.Now.ToString("HH:mm:ss.ffffff ");
        }

        private static readonly string productName = "MultithreadWindowsCopy";

        static void Main(string[] args)
        {             
            // Used for debbuging.
            Debug.Listeners.Add(new TextWriterTraceListener(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), productName, "debug.log")));
            Debug.AutoFlush = true;

            Debug.WriteLine(Environment.NewLine + CurrentTime() + "PasteApp started");

            // Obtain destination folder from arguments.
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
            string[] files = foldersAndFiles.Where(item => File.Exists(Path.Combine(root, item))).ToArray();
            string[] folders = foldersAndFiles.Where(item => Directory.Exists(Path.Combine(root, item))).ToArray();

            Debug.WriteLine(CurrentTime() + "Items scheduled for pasting:");
            Debug.WriteLine(root + @"\");
            Debug.Indent();
            foreach (string file in files)
                Debug.WriteLine(file);
            foreach (string folder in folders)
                Debug.WriteLine(folder);
            Debug.Unindent();


            long totalFileCount = GetTotalFileCount(root, foldersAndFiles);
            long totalFileSize = GetTotalFileSize(root, foldersAndFiles);

            Debug.WriteLine(CurrentTime() + "Total file count: " + totalFileCount);
            Debug.WriteLine(CurrentTime() + "Total file size: " + totalFileSize);

            // Create robocopy script.
            // Commands to execute.
            List<string> commands = new List<string>();
            string options = @"/nc /ndl /fp /bytes";
            foreach (string file in files)
            {
                string filePath = Path.Combine(root, file);
                if (File.Exists(filePath) == true)
                    commands.Add(QuoteEnclose(root) + " " + QuoteEnclose(destination) + " " + QuoteEnclose(file) + " " + options);
                else
                    throw new Exception("The file " + filePath + " doesn't exist!");
            }
            foreach (string folder in folders)
            {
                string folderPath = Path.Combine(root, folder);
                if (Directory.Exists(folderPath) == true)
                    commands.Add(QuoteEnclose(folderPath) + " " + QuoteEnclose(Path.Combine(destination, folder)) + " /e" + " " + options);
                else
                    throw new Exception("The folder " + folderPath + " doesn't exist!");
            }

            string copyScript = string.Join(Environment.NewLine, commands.ToArray());
            Debug.WriteLine(CurrentTime() + "Script to execute:");
            Debug.WriteLine(copyScript);

            
            CopyDialog copyDialog = new CopyDialog(totalFileCount, totalFileSize, root, destination);
            Thread UIThread = new Thread(() => Application.Run(copyDialog));
            UIThread.Start();
            
            // Run robocopy script
            Debug.WriteLine(CurrentTime() + "Executing script...");
            foreach (string command in commands) {
                using (Process p = new Process())
                {
                    p.StartInfo.FileName = "robocopy.exe";
                    p.StartInfo.Arguments = command;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardInput = true;
                    p.StartInfo.RedirectStandardError = true;
                    p.StartInfo.RedirectStandardOutput = true;

                    p.OutputDataReceived += copyDialog.RobocopyOutputHandler;
                    p.ErrorDataReceived += copyDialog.RobocopyErrorHandler;

                    p.OutputDataReceived += (s, e) => Debug.WriteLine(e.Data);
                    p.ErrorDataReceived += (s, e) => Debug.WriteLine(e.Data);

                    p.Start();
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                    
                    p.WaitForExit();
                }
            }
            
            Debug.WriteLine(CurrentTime() + "Done");

            Debug.WriteLine(CurrentTime() + "PasteApp finished.");

            UIThread.Join();
        }

        private static void P_Exited(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
