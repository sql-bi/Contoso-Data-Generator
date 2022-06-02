using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DatabaseGenerator
{
    public class Logger
    {
        private static object _multiThreadLock = new object();
        private static StreamWriter _streamWriter;
        private static DateTime _previousDT = DateTime.UtcNow;

        public static void Init(string fileName)
        {
            _streamWriter = new StreamWriter(fileName);
            _streamWriter.AutoFlush = true;  // because this is a log file
        }

        public static void Info(string msg)
        {
            lock (_multiThreadLock)
            {
                double elapsed = DateTime.UtcNow.Subtract(_previousDT).TotalSeconds;
                string txt = $"{DateTime.UtcNow.ToString("O")} | {elapsed.ToString("0.00")} > {msg}";
                Console.WriteLine(txt);
                _streamWriter.WriteLine(txt);
                _streamWriter.Flush();
                _previousDT = DateTime.UtcNow;
            }
        }
    }
}
