using System;
using System.IO;

namespace BetterImageTagEditor
{
    public enum MessageType { Info = 0, Warning = 1, Error = 2 }

    public static class Log
    {
        public static string LogPath;
        public static bool LogMessageTypes = true;
        public static bool LogTimestamps = true;

        private static StreamWriter sw;

        public static void SetLogPath(string path)
        {
            LogPath = path;
            sw = new StreamWriter(new FileStream(LogPath, FileMode.Create));
        }
        
        public static void Write(string message, MessageType type = MessageType.Info)
        {
            if (LogTimestamps)
            {
                sw.Write(DateTime.Now + " - ");
            }
            if (LogMessageTypes)
            {
                sw.Write(type + " - ");
            }
            sw.Write(message);
            sw.Flush();
        }

        public static void WriteLine(string message, MessageType type = MessageType.Info)
        {
            if (LogTimestamps)
            {
                sw.Write(DateTime.Now + " - ");
            }
            if (LogMessageTypes)
            {
                sw.Write(type + " - ");
            }
            sw.WriteLine(message);
            sw.Flush();
        }
    }
}
