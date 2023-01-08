using System.Collections.Generic;
using Newtonsoft.Json;

namespace RustyWatcher.Configurations;

public class SimpleLinkConfiguration
{
    [JsonProperty("Use")] 
    public bool Use;
    
    [JsonProperty("Endpoint")]
    public string LinkingEndpoint = string.Empty;    
    
    [JsonProperty("API Key")]
    public string LinkingApiKey = string.Empty;

    [JsonProperty("Main Guild Id")] 
    public ulong GuildId;

    [JsonProperty("Force Sync (default should be false, force sync doesn't care about the users current roles)")] 
    public bool ForceSync = true;
    
    [JsonProperty("Role Syncing (Key: Discord Role Id, Value: In-Game Group Name)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public Dictionary<ulong, string> RoleSyncing = new() { { 0, "default" }};

    [JsonProperty("Linking Group In-Game")]
    public string LinkingGroupInGame = "linked";
}