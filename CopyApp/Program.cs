using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace CopyApp
{
    class Program
    {
        // dll used to expose windows exlporer api.
        // For documentation see "user32.dll" on https://www.pinvoke.net/index.aspx
        [DllImport("User32.dll")]
        public static extern IntPtr GetForegroundWindow();

        // Only one instance of the app should execute at any time.
        // Primary reason is multiple instances of the app being started when the app
        // is called from the windows exlporer context menu for a selection of multiple files.
        private static readonly Mutex oneAppInstanceMutex = new Mutex(true, @"CopyApp_48031724+Jovan-Zan@users.noreply.github.com");

        private static readonly string productName = "MultithreadWindowsCopy";

        /// <summary>
        /// Formatted current time.
        /// </summary>
        /// <returns>String representing date and tim in format "hh:mm:ss.ffffff ".</returns>
        public static string CurrentTime()
        {
            return DateTime.Now.ToString("hh:mm:ss.ffffff ");
        }
        
        static void Main(string[] args)
        {
            // If mutex cannot be aquired exit the app.
            if (oneAppInstanceMutex.WaitOne(0) == false)
                return;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // Used to store paths to files and folders selected for copying.
            string outputFileDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), productName);
            string outputFile = Path.Combine(outputFileDirectory, "filesToCopy.out");
            Directory.CreateDirectory(outputFileDirectory);

            // Used for debbuging.
            Debug.Listeners.Add(new TextWriterTraceListener(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), productName, "debug.log")));
            Debug.AutoFlush = true;

            Debug.WriteLine(Environment.NewLine + CurrentTime() + "CopyApp started");

            using (StreamWriter outputStream = new StreamWriter(outputFile, false))
            {
                IntPtr foregroundWindowHandle = GetForegroundWindow();

                Debug.WriteLine(CurrentTime() + "Foreground window handle AS IntPtr: " + foregroundWindowHandle);
                Debug.WriteLine(CurrentTime() + "Foreground window handle AS int: " + foregroundWindowHandle.ToInt32());

                foreach (SHDocVw.InternetExplorer window in new SHDocVw.ShellWindows())
                {
                    // Debug information.
                    Debug.WriteLine(CurrentTime() + "Found new explorer window:");
                    Debug.Indent();
                    Debug.WriteLine("LocationName: " + window.LocationName);
                    Debug.WriteLine("Path: " + window.Path);
                    Debug.WriteLine("Handle: " + window.HWND);

                    if (window.HWND == (int)foregroundWindowHandle)
                    {
                        outputStream.WriteLine(((Shell32.IShellFolderViewDual2)window.Document).Folder.Items().Item().Path);
                        Debug.WriteLine("Found foreground folder.");
                        Debug.WriteLine("Found items: ");

                        Debug.Indent();
                        Shell32.FolderItems items = ((Shell32.IShellFolderViewDual2)window.Document).SelectedItems();
                        foreach (Shell32.FolderItem item in items)
                        {
                            outputStream.WriteLine(item.Name);
                            Debug.WriteLine(item.Name);
                        }
                        Debug.Unindent();
                    }

                    Debug.Unindent();
                }

                stopwatch.Stop();
                Debug.WriteLine(CurrentTime() + "Miliseconds elapsed: " + stopwatch.ElapsedMilliseconds);

                outputStream.Close();
            }

            // Release the mutex
            oneAppInstanceMutex.ReleaseMutex();

            Debug.WriteLine(CurrentTime() + "CoppyApp finished.");
        } 
    }
}
