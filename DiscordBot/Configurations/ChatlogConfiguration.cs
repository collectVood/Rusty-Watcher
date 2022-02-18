using System.Collections.Generic;
using Newtonsoft.Json;

namespace RustyWatcher.Configurations;

public class ChatlogConfiguration
{
    [JsonProperty("Use Chatlog")]
    public bool Use = false;
    
    [JsonProperty("Chatlog Webhook Url")]
    public string WebhookUrl = string.Empty;
    
    [JsonProperty("Command Prefix")]
    public string CommandPrefix = "!";
    
    [JsonProperty("Require Chatlog Confirmation")]
    public bool ChatlogConfirmation = true;
    
    [JsonProperty("Chatlog Channel Id")]
    public ulong ChannelId = 0;
    
    [JsonProperty("Default Name Color (for send messages when no steamId provided)")]
    public string DefaultNameColor = "#af5";
    
    [JsonProperty("Server Message Colour (RGB)")]
    
    public RGBConfiguration ServerMessageColour = new()
    { 
        Red = 255, 
        Blue = 0, 
        Green = 0
    };
    
    [JsonProperty("Can Use Commands Role Ids", ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public List<ulong> CanUseCommandsRoleIds = new() 
    { 
        0, 0
    };
    
    [JsonProperty("Show team chat")]
    public bool ShowTeamChat = true;
}
