using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;
using RustyWatcher.Data;

namespace RustyWatcher
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

            if (string.IsNullOrEmpty(Program.Data.SteamAPIKey))
            {
                Log.Warning("No Steam API Key set! (Ignore this if you are not using the chatlog feature)");
            }

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
        }

        #endregion

        #region Init

        private static void Main()
        {
            try
            {
                Console.Title = "Rusty Watcher made by @collect_vood#3773";

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
            catch (Exception e)
            {
                Log.Error(e);
            }

            Console.ReadKey();
        }

        #endregion

        #region Methods

        private static void EnableOutputFile()
        {
            if (!Data.CreateOutputfile) 
                return;

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

        public static ulong TryGetSteamId(ulong discordId)
        {
            if (!Data.DiscordSteamIds.TryGetValue(discordId, out var steamId))
            {
                steamId = 0;
                Log.Warning("No steamId found for user " + discordId + " consider setting one!");
            }

            return steamId;
        }

        #endregion
    }
}
