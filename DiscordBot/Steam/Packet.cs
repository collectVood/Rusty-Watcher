using System.Collections.Generic;
using Newtonsoft.Json;

namespace RustyWatcher.Steam
{
    public class SteamRootObject
    {
        [JsonProperty("response")]
        public Results Results { get; set; }
    }

    public class Results
    {
        [JsonProperty("players")]
        public List<Players> Players { get; set; }
    }

    public class Players
    {
        [JsonProperty("steamid")]
        public string SteamID { get; set; }
        [JsonProperty("communityvisibilitystate")]
        public int CommunityVisibilityState { get; set; }
        [JsonProperty("profilestate")]
        public int Profilestate { get; set; }
        [JsonProperty("personaname")]
        public string PersonaName { get; set; }
        [JsonProperty("lastlogoff")]
        public uint LastLogoff { get; set; }
        [JsonProperty("commentpermission")]
        public int CommentPermission { get; set; }
        [JsonProperty("profileurl")]
        public string ProfileUrl { get; set; }
        [JsonProperty("avatar")]
        public string Avatar { get; set; }
        [JsonProperty("avatarmedium")]
        public string AvatarMedium { get; set; }
        [JsonProperty("avatarfull")]
        public string AvatarFull { get; set; }
        [JsonProperty("personastate")]
        public int PersonaState { get; set; }
        [JsonProperty("primaryclanid")]
        public string PrimaryClanid { get; set; }
        [JsonProperty("timecreated")]
        public uint TimeCreated { get; set; }
        [JsonProperty("personastateflags")]
        public int PersonaStateFlags { get; set; }
        [JsonProperty("gameserverip")]
        public string GameserverIP { get; set; }
        [JsonProperty("gameserversteamid")]
        public string GameserverSteamID { get; set; }
        [JsonProperty("gameextrainfo")]
        public string GameExtraInfo { get; set; }
        [JsonProperty("gameid")]
        public string GameID { get; set; }
        [JsonProperty("loccountrycode")]
        public string LocCountryCode { get; set; }
    }    
}
