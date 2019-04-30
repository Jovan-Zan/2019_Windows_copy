using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace CopyApp
{
    class Program
    {
        // User32.dll is used to expose windows exlporer api.
        // For documentation see "user32.dll" on https://www.pinvoke.net/index.aspx
        
        /// <summary>
        /// Gets the handle of window that is currently running in the foreground.
        /// This method is used to obtain source folder.
        /// </summary>
        /// <returns> Handle to the foreground window.</returns>
        [DllImport("User32.dll")]
        public static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// From docs.microsoft.com:
        /// The GetDesktopWindow function returns a handle to the desktop window. 
        /// The desktop window covers the entire screen. 
        /// The desktop window is the area on top of which other windows are painted.
        /// </summary>
        /// <returns>Handle to the desktop.</returns>
        [DllImport("User32.dll", SetLastError = false)]
        static extern IntPtr GetDesktopWindow();

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
            return DateTime.Now.ToString("HH:mm:ss.ffffff ");
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
                IntPtr desktopWindowHandle = GetDesktopWindow();

                // Flag used to determine where CopyApp was called from Explorer window
                // or straight from Desktop.
                bool copyingFromExplorerWindow = false;

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
                        copyingFromExplorerWindow = true;

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

                if (copyingFromExplorerWindow == false)
                {
                  // TODO
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
