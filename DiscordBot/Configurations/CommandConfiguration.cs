using System.Collections.Generic;
using Newtonsoft.Json;

namespace RustyWatcher.Configurations;

public class CommandConfiguration
{
    [JsonProperty(PropertyName = "Display Name")] 
    public string DisplayName = "Mute";

    [JsonProperty(PropertyName = "Name (i.e. for a mute command this would be 'mute', NEVER leave empty unless raw command)")] 
    public string Name = "mute";
    
    [JsonProperty(PropertyName = "Limited to Role Ids (null for anyone)")]
    public List<ulong>? RolesIds = new();
}