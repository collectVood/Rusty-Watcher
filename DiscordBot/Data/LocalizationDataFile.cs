using Newtonsoft.Json;

namespace RustyWatcher.Data
{
    public class LocalizationDataFile
    {
        [JsonProperty(PropertyName = "Player Count Status")]
        public string PlayerStatus = "{0} / {1} {2}";         
        [JsonProperty(PropertyName = "Embed Title")]
        public string EmbedTitle = "{0} {1}";        
        [JsonProperty(PropertyName = "Embed Description")]
        public string EmbedDescription = "{0}:{1}";        
        [JsonProperty(PropertyName = "Embed Footer")]
        public string EmbedFooter = "Last Wiped {0}";        
        [JsonProperty(PropertyName = "Embed Field Player")]
        public EmbedField EmbedFieldPlayer = new EmbedField()
        {
            EmbedName = "Players",
            EmbedValue = "{0}"
        };        
        [JsonProperty(PropertyName = "Embed Field FPS")]
        public EmbedField EmbedFieldFPS = new EmbedField()
        {
            EmbedName = "FPS",
            EmbedValue = "{0}"
        };        
        [JsonProperty(PropertyName = "Embed Field Entities")]
        public EmbedField EmbedFieldEntities = new EmbedField()
        {
            EmbedName = "Entities",
            EmbedValue = "{0}"
        };        
        [JsonProperty(PropertyName = "Embed Field Game time")]
        public EmbedField EmbedFieldGametime = new EmbedField()
        {
            EmbedName = "Game time",
            EmbedValue = "{0}"
        };        
        [JsonProperty(PropertyName = "Embed Field Uptime")]
        public EmbedField EmbedFieldUptime = new EmbedField()
        {
            EmbedName = "Uptime",
            EmbedValue = "{0}"
        };        
        [JsonProperty(PropertyName = "Embed Field Map")]
        public EmbedField EmbedFieldMap = new EmbedField()
        {
            EmbedName = "Map",
            EmbedValue = "[View here]({0})"
        };
    }

    public class EmbedField
    {
        [JsonProperty(PropertyName = "Name")]
        public string EmbedName;
        [JsonProperty(PropertyName = "Value")]
        public string EmbedValue;        
        [JsonProperty(PropertyName = "Inline")]
        public bool EmbedInline = true;
    }
}
