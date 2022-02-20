using Newtonsoft.Json;

namespace RustyWatcher.Configurations;

public class ServerConfiguration
{
    [JsonProperty("Name")] 
    public string Name = string.Empty;
    
    [JsonProperty("Discord")]
    public DiscordConfiguration Discord = new();
    
    [JsonProperty("Rcon")]
    public RconConfiguration Rcon = new();
    
    [JsonProperty("Chatlog")]
    public ChatlogConfiguration Chatlog = new(); 
    
    [JsonProperty("Join/Disconnect Log")]
    public JDLogConfiguration JDLog = new();
    
    [JsonProperty("Serverinfo")]
    public ServerInfoConfiguration ServerInfo = new();
    
    [JsonProperty("Localization")]
    public LocalizationConfiguration Localization = new();
}