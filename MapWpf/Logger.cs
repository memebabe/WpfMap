using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MapWpf
{
    public class Logger
    {
        private static string LogFolder = Directory.GetCurrentDirectory() + @"\Logs\";
        private static object SyncObject = new object();
        private static Queue<string> Logs = new Queue<string>();
        
        public static async void Log(string clazz, string msg)
        {
            if (null != msg)
            {
                lock (SyncObject)
                {
                    string text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + clazz + "\t:" + msg;
                    Logs.Enqueue(text);
                }
                await WakeMeUp();
            }
        }

        private static bool Sleeping = true;
        private static async Task WakeMeUp()
        {
            if (!Sleeping)
                return;
            else
                Sleeping = false;

            string logFileName = LogFolder + DateTime.Now.ToString("yyyyMMdd_HH") + ".log";
            if (!Directory.Exists(LogFolder))
            {
                Directory.CreateDirectory(LogFolder);
            }

            using (StreamWriter writer = File.AppendText(logFileName))
            {
                string text;
                while (Logs.Count > 0)
                {
                    lock (SyncObject)
                        text = Logs.Dequeue();
                    writer.WriteLine(text);
#if DEBUG
                    System.Console.WriteLine(text);
#endif
                }
            }
            Sleeping = true;
        }
    }
}
