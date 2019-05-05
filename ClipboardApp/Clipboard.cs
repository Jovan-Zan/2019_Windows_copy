using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;

namespace ClipboardApp
{
    public class Clipboard
    {
        public static void Main(string[] args)
        {
            const int mmfMaxSize = 16 * 1024 * 1024;
            MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen("ClipboardAppMemoryMappedFile", mmfMaxSize, MemoryMappedFileAccess.ReadWrite);


            // The memory mapped file lives as long as this process is running.
            // For that purpose, ClipboardApp runs indefinetly.

            // From the Microsoft docs:
            // Begins running a standard application message loop 
            // on the current thread, without a form.
            System.Windows.Forms.Application.Run();
        }
    }
}
