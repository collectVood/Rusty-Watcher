using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;
using DiscordBot.Data;

namespace DiscordBot
{
    public class Program
    {
        #region Fields

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };

        public static DataFile Data = new DataFile();

        public static StreamWriter OutputText = null;

        public static string FilesDirectory = string.Empty;

        #endregion

        #region Constructor

        private Program()
        {
            EnableOutputFile();

            try
            {
                var tasks = new List<Task>();
                foreach (var server in Data.ServerData)
                {
                    tasks.Add(new Bot(server).CreateBot());
                }
                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            Console.ReadKey();
        }

        #endregion

        #region Init

        private static void Main()
        {
            FilesDirectory = Path.Combine(GetBasePath(), "Files");
            if (!Directory.Exists(FilesDirectory))
            {
                Directory.CreateDirectory(FilesDirectory);
            }
            
            //config
            string path = Path.Combine(FilesDirectory, "config.json");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                Data = JsonConvert.DeserializeObject<DataFile>(json);
                Log.Info("Config found & loaded!");
            }
            else
            {
                Log.Info("No config found, creating one!");
                Data = new DataFile();
            }

            File.WriteAllText(path, JsonConvert.SerializeObject(Data, JsonSettings));

            new Program();
        }

        #endregion

        #region Methods

        private static void EnableOutputFile()
        {
            if (!Data.CreateOutputfile) return;

            try
            {
                string path = Path.Combine(FilesDirectory, "output.txt");

                OutputText = new StreamWriter(path);

                Log.Info("Output file path : " + path);
            }
            catch (Exception e)
            {
                Log.Error("Cannot open output.txt for writing.");
                Log.Error(e);
            }
        }

        private static string GetBasePath()
        {
            using var processModule = Process.GetCurrentProcess().MainModule;
            return Path.GetDirectoryName(processModule?.FileName);
        }

        #endregion
    }
}
