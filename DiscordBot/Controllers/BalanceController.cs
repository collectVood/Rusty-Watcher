using System;
using System.Threading.Tasks;
using Discord.Webhook;
using RustyWatcher.Configurations;
using Serilog;

namespace RustyWatcher.Controllers;

public class BalanceController
{
    private readonly float[] _prevFps = new float[100];
    private int _prevFpsWrite = 0;
    private bool _activeSpike;
    
    private readonly Connector _connector;
    private readonly BalancingConfiguration _config;
    private readonly DiscordWebhookClient? _webhook;
    
    private static readonly ILogger _logger = Log.ForContext<BalanceController>();
    
    public BalanceController(Connector connector, BalancingConfiguration configuration)
    {
        _connector = connector;
        _config = configuration;

        if (!string.IsNullOrEmpty(_config.SpikeDiscordWebhook))
            _webhook = new DiscordWebhookClient(_config.SpikeDiscordWebhook);
    }
    
    public void AddFps(float fps)
    {
        if (_prevFpsWrite >= _prevFps.Length)
            _prevFpsWrite = 0;

        _prevFps[_prevFpsWrite++] = fps;
        
        if (_activeSpike)
            return;
        
        // Check if a spike is detected by using percentage of current fps compared to avg fps
        var avgFps = GetAvgFps();
        var percentageFps = fps / avgFps;
        
        _logger.Debug("Adding Fps {fps} - Avg Fps {avgFps} - Percentage Fps {percentageFps}", 
            fps, avgFps, percentageFps);
        
        // current fps was higher than last, so its OK
        if (percentageFps > 1)
            return;

        if (percentageFps > _config.MinAvgFpsDiffer)
            return;

        _activeSpike = true;
        
        _ = Task.Run(() => HandleSpike(avgFps, fps, percentageFps));
        _ = Task.Run(ResetSpike);
    }

    private float GetAvgFps()
    {
        var avgFps = 0f;
        var count = 0;
        
        foreach (var iterationFps in _prevFps)
        {
            // partially filled array
            if (iterationFps == 0f)
            {
                break;
            }
            
            avgFps += iterationFps;
            count++;
        }

        avgFps /= count;

        return avgFps;
    }

    private async Task HandleSpike(float avgFps, float spikeFps, float percentageFps)
    { 
        _logger.Information("Active Spike Detected - Fps {fps} - Avg Fps {avgFps} - Percentage Fps {percentageFps}", 
            spikeFps, avgFps, percentageFps);
        
        foreach (var command in _config.SpikeRunCommands)
            _connector.SendCommandRcon(command, null);
        
        if (_webhook != null)
        {
            await _webhook.SendMessageAsync(
                string.Format(_config.SpikeMessage, DateTimeOffset.Now.ToUnixTimeSeconds(), avgFps, spikeFps));
        }
    }

    private async Task ResetSpike()
    {
        _logger.Information("Queued reset spike...");
        
        await Task.Delay(_config.SpikeReset);

        _activeSpike = false;
        
        foreach (var command in _config.SpikeRestoreRunCommands)
            _connector.SendCommandRcon(command, null);

        _logger.Information("Reset Spike...");
        
        if (_webhook != null)
        {
            await _webhook.SendMessageAsync(string.Format(_config.SpikeRevertMessage, DateTimeOffset.Now.ToUnixTimeSeconds()));
        }
    }
}