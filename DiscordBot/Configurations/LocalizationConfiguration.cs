using Newtonsoft.Json;

namespace RustyWatcher.Configurations;

public class LocalizationConfiguration
{
    [JsonProperty(PropertyName = "Embed Title")]
    public string EmbedTitle = "{0} {1}";        
    
    [JsonProperty(PropertyName = "Embed Description")]
    public string EmbedDescription = "{0}:{1}";   
    
    [JsonProperty(PropertyName = "Embed Footer")]
    public string EmbedFooter = "Last Wiped {0}"; 
    
    [JsonProperty(PropertyName = "Embed Field Player")]
    public EmbedField EmbedFieldPlayer = new()
    {
        EmbedName = "Players",
        EmbedValue = "{0}"
    };  
    
    [JsonProperty(PropertyName = "Embed Field FPS")]
    public EmbedField EmbedFieldFPS = new()
    {
        EmbedName = "FPS",
        EmbedValue = "{0}"
    }; 
    
    [JsonProperty(PropertyName = "Embed Field Entities")]
    public EmbedField EmbedFieldEntities = new()
    {
        EmbedName = "Entities",
        EmbedValue = "{0}"
    };    
    
    [JsonProperty(PropertyName = "Embed Field Game time")]
    public EmbedField EmbedFieldGametime = new()
    {
        EmbedName = "Game time",
        EmbedValue = "{0}"
    };   
    
    [JsonProperty(PropertyName = "Embed Field Uptime")]
    public EmbedField EmbedFieldUptime = new()
    {
        EmbedName = "Uptime",
        EmbedValue = "{0}"
    };  
    
    [JsonProperty(PropertyName = "Embed Field Map")]
    public EmbedField EmbedFieldMap = new()
    {
        EmbedName = "Map",
        EmbedValue = "[View here]({0})"
    };
    
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

