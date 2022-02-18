using Newtonsoft.Json;
using Serilog.Events;

namespace RustyWatcher.Configurations;

public class LogConfiguration
{
    [JsonProperty("File Logging")]
    public bool FileLogging = true;    
    
    [JsonProperty("Level Logging")] 
    public LogEventLevel LevelLogging = LogEventLevel.Information;
}