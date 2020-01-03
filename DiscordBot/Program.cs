using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DiscordBot
{
    public class Program
    {
        #region Data

        public class Data
        {
            [JsonProperty("Discord refresh (seconds)")]
            public int DiscordDelay = 60;
            [JsonProperty("Reconnect delay (seconds)")]
            public int ReconnectDelay = 60;
            [JsonProperty("Servers", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<ServerData> ServerData = new List<ServerData> { new ServerData() };
            [JsonProperty("Debug")]
            public bool Debug = false;
        }

        public class ServerData
        {
            [JsonProperty("Token")]
            public string Token = string.Empty;
            [JsonProperty("Rcon IP")]
            public string RconIP = string.Empty;
            [JsonProperty("Rcon Port")]
            public string RconPort = string.Empty;
            [JsonProperty("Rcon Password")]
            public string RconPW = string.Empty;
        }

        #endregion

        public static Data _data = new Data();

        private Program()
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var server in _data.ServerData)
                {
                    tasks.Add(new Bot(server).CreateBot());
                }
                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                Log.Error(e.Message, e.StackTrace);
            }
            Console.ReadKey();
        }

        private static void Main()
        {
            string directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            string path = Path.Combine(directory, "config.json");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                _data = JsonConvert.DeserializeObject<Data>(json);
                Log.Info("Config found & loaded!");
            }
            else
            {
                Log.Info("No config found, creating one!");
                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented
                };
                File.WriteAllText(path, JsonConvert.SerializeObject(_data, settings));
            }

            new Program();
        }
    }
}
