using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
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

        public static void WriteToMMF(string[] lines)
        {
            const int mmfMaxSize = 16 * 1024 * 1024;  // allocated memory for this memory mapped file (bytes)
            const int mmfViewSize = 16 * 1024 * 1024; // how many bytes of the allocated memory can this process access

            MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen("ClipboardAppMemoryMappedFile", mmfMaxSize, MemoryMappedFileAccess.ReadWrite);
            MemoryMappedViewStream mmvStream = mmf.CreateViewStream(0, mmfViewSize);
            BinaryFormatter formatter = new BinaryFormatter();

            formatter.Serialize(mmvStream, lines);
            mmvStream.Seek(0, SeekOrigin.Begin);
        }


        // HARDCODED!!!
        // Used during development, when apps are in different folders.
        private static string clipboardAppLocation = @"C:\Users\toshiba\Desktop\Programske paradigme (PP)\Windows_copy\ClipboardApp\bin\Debug\ClipboardApp.exe";

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

            // Used for debbuging.
            Debug.Listeners.Add(new TextWriterTraceListener(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), productName, "debug.log")));
            Debug.AutoFlush = true;
            
            Debug.WriteLine(Environment.NewLine + CurrentTime() + "CopyApp started");
            Debug.WriteLine(CurrentTime() + Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            // clipboardAppLocation = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "ClipboardApp.exe");


            // Start ClipboardApp.exe
            if (Process.GetProcessesByName("ClipboardApp").Length == 0)
            {
                Process clipboardApp = new Process();
                clipboardApp.StartInfo.FileName = clipboardAppLocation;
                clipboardApp.StartInfo.CreateNoWindow = true;
                clipboardApp.StartInfo.UseShellExecute = false;
                clipboardApp.Start();

                Debug.WriteLine(CurrentTime() + "ClipboardApp started, ClipboardAppName = " + clipboardApp.ProcessName);
            }
            
            IntPtr foregroundWindowHandle = GetForegroundWindow();
            IntPtr desktopWindowHandle = GetDesktopWindow();

            // Flag used to determine where CopyApp was called from Explorer window
            // or straight from Desktop.
            bool copyingFromExplorerWindow = false;

            Debug.WriteLine(CurrentTime() + "Foreground window handle AS IntPtr: " + foregroundWindowHandle);
            Debug.WriteLine(CurrentTime() + "Foreground window handle AS int: " + foregroundWindowHandle.ToInt32());

            // Iterate trough explorer windows and find the foreground window.
            // Get the selected items from the foregound window.
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
                    List<string> filesToCopy = new List<string>();

                    filesToCopy.Add(((Shell32.IShellFolderViewDual2)window.Document).Folder.Items().Item().Path);

                    Debug.WriteLine("Found foreground folder.");
                    Debug.WriteLine("Found items: ");

                    Debug.Indent();
                    Shell32.FolderItems items = ((Shell32.IShellFolderViewDual2)window.Document).SelectedItems();
                    foreach (Shell32.FolderItem item in items)
                    {
                        filesToCopy.Add(item.Name);
                        Debug.WriteLine(item.Name);
                    }

                    WriteToMMF(filesToCopy.ToArray());

                    Debug.Unindent();
                }

                Debug.Unindent();
            }

            if (copyingFromExplorerWindow == false)
            {
                // TODO
            }
            
            // Release the mutex
            oneAppInstanceMutex.ReleaseMutex();

            stopwatch.Stop();
            Debug.WriteLine(CurrentTime() + "Miliseconds elapsed: " + stopwatch.ElapsedMilliseconds);
            Debug.WriteLine(CurrentTime() + "CoppyApp finished.");
        } 
    }
}
