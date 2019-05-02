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
        const int mmfMaxSize = 1024;  // allocated memory for this memory mapped file (bytes)
        const int mmfViewSize = 1024; // how many bytes of the allocated memory can this process access

        private static MemoryMappedFile mmf;
        private static MemoryMappedViewStream mmvStream;
        private static BinaryFormatter formatter;

        
        public static void WriteToMMF(string [] lines)
        {
            formatter.Serialize(mmvStream, lines);
            mmvStream.Seek(0, SeekOrigin.Begin);
        }

        public static string [] ReadFromMMF()
        {
            string [] lines = (string[]) formatter.Deserialize(mmvStream);
            mmvStream.Seek(0, SeekOrigin.Begin);
            return lines;
        }

        public static void Main(string[] args)
        {
            mmf = MemoryMappedFile.CreateOrOpen("mmf", mmfMaxSize, MemoryMappedFileAccess.ReadWrite);
            mmvStream = mmf.CreateViewStream(0, mmfViewSize);
            formatter = new BinaryFormatter();

            // the memory mapped file lives as long as this process is running
            while (true) ;
        }
    }
}
