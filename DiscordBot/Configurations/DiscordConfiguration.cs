using System.Collections.Generic;
using Newtonsoft.Json;

namespace RustyWatcher.Configurations;

public class DiscordConfiguration
{
    [JsonProperty("Bot Token")]
    public string Token = string.Empty;   
    
    [JsonProperty("Guild Id (required for guild only slash commands)")]
    public ulong GuildId;  
    
    [JsonProperty("Activity type (0 = Playing, 1 = Streaming, 2 = Listening, 3 = Watching)")]
    public int ActivityType;
    
    [JsonProperty(PropertyName = "Custom Commands", ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public List<CommandConfiguration> Commands = new() { 
        new CommandConfiguration() { DisplayName = "Raw", Name = string.Empty }, 
        new CommandConfiguration()
    };

    [JsonProperty(PropertyName = "Administrative Commands Role Ids (reconnect, etc)")]
    public List<ulong> AdministrativeCommandRoleIds = new();
}