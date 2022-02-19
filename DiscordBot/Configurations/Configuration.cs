using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using RustyWatcher.Helpers;

namespace RustyWatcher.Configurations;

public class Configuration
{
    [JsonProperty("Steam API Key (for avatars)")]
    public string SteamAPIKey = string.Empty;
    
    [JsonProperty("Linking Endpoint")]
    public string LinkingEndpoint = string.Empty;    
    
    [JsonProperty("Linking API Key")]
    public string LinkingApiKey = string.Empty;

    [JsonProperty("Main Guild Id")] 
    public ulong GuildId;
    
    [JsonProperty("Nitro Role Id")] 
    public ulong NitroRoleId;

    [JsonProperty("Update Delay (seconds)")]
    public int UpdateDelay = 15;
    
    [JsonProperty("Influx Database (for example for Grafana)")]
    public InfluxDbConfiguration InfluxDbConfiguration = new InfluxDbConfiguration();
    
    [JsonProperty("Servers", ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public List<ServerConfiguration> Servers = new() { new ServerConfiguration() };
    
    [JsonProperty("Staff Discord & SteamIds (Key: DiscordId; Value: SteamId)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public Dictionary<ulong, ulong> DiscordSteamIds = new()
    {
        { 0, 0 }
    };

    [JsonProperty("Logging", ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public LogConfiguration LogConfiguration = new();
    
    [JsonIgnore] public static Configuration Instance;
    
    [JsonIgnore] private const string CONFIG_FOLDER = "Configuration";
    [JsonIgnore] private const string CONFIG_FILE = "config.json";
    [JsonIgnore] private static string _configPath;
    
    public static void Load()
    {
        var configDirectyPath = Path.Combine(Utilities.GetBasePath(), CONFIG_FOLDER);
        if (!Directory.Exists(configDirectyPath))
            Directory.CreateDirectory(configDirectyPath);

        _configPath = Path.Combine(configDirectyPath, CONFIG_FILE);
        if (File.Exists(_configPath))
        {
            var json = File.ReadAllText(_configPath);
            Instance = JsonConvert.DeserializeObject<Configuration>(json);
            Instance.Save();
        }
        else
        {
            Instance = new Configuration();
            Instance.Save();
        }
    }

    public void Save()
    {
        File.WriteAllText(_configPath, JsonConvert.SerializeObject(this, Formatting.Indented));
    }
}
