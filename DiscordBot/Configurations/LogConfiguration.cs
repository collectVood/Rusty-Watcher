using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog.Events;

namespace RustyWatcher.Configurations;

public class LogConfiguration
{
    [JsonProperty("File Logging")]
    public bool FileLogging = true;    
    
    [JsonProperty("Level Logging (Verbose, Debug, Information, Warning, Error, Fatal)")]
    [JsonConverter(typeof(StringEnumConverter))]
    public LogEventLevel LevelLogging = LogEventLevel.Information;
}