using Discord;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RustyWatcher.Configurations;

public class UserStatusConfiguration
{
    [JsonProperty("Status Message")]
    public string Message = null!;
    
    [JsonProperty("Status Type (Offline, Online, Idle, AFK, DoNotDisturb, Invisible)")]
    [JsonConverter(typeof(StringEnumConverter))]
    public UserStatus Type;

    // For Json
    public UserStatusConfiguration() {}
    
    public UserStatusConfiguration(string msg, UserStatus type)
    {
        Message = msg;
        Type = type;
    }
}