using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using RustyWatcher.Configurations;
using RustyWatcher.Helpers;
using RustyWatcher.Models.Rcon;
using RustyWatcher.Workers;
using Serilog;

namespace RustyWatcher.Controllers;

public class Connector
{
    #region Fields

    private static readonly ILogger _logger = Log.ForContext<Connector>();
    private static readonly List<Connector> _connectors = new();
    private static DiscordGlobalWorker? _globalDiscordWorker;

    private readonly ServerConfiguration _configuration;
    private readonly DiscordWorker _discordWorker;
    private readonly RconWorker _rconWorker;
    private readonly BalanceController? _balanceController;
    private readonly SpamHandler _spamHandler;

    #endregion
    
    private Connector(ServerConfiguration serverConfiguration)
    {
        _configuration = serverConfiguration;
        
        _discordWorker = new DiscordWorker(this, serverConfiguration);
        _rconWorker = new RconWorker(this, serverConfiguration.Rcon);
        _spamHandler = new SpamHandler(this);
        
        if (serverConfiguration.Balance.Use)
            _balanceController = new BalanceController(this, _configuration.Balance);
        
        if (_configuration.Population.Enabled)
            Task.Run(TryGrowth);
    }

    #region Init

    public static void StartAll()
    {
        Log.Information("Starting Connectors...");
        
        foreach (var serverConfiguration in Configuration.Instance.Servers)
            _connectors.Add(new Connector(serverConfiguration));

        if (!Configuration.Instance.GlobalServerBot.Enabled)
            return;

        _globalDiscordWorker = new DiscordGlobalWorker(_connectors, Configuration.Instance.GlobalServerBot);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Sends a Message to Rcon with Player details (name, steamId)
    /// </summary>
    /// <param name="message"></param>
    /// <param name="username"></param>
    /// <param name="discordId"></param>
    /// <returns></returns>
    public bool SendMessageRcon(string message, string username, ulong discordId)
    {
        Configuration.Instance.DiscordSteamIds.TryGetValue(discordId, out var steamId);

        return _rconWorker.SendMessage(message, username, _configuration.Chatlog.DefaultNameColor, steamId);
    }
    
    /// <summary>
    /// Sends a Command to Rcon with Callback of the Command
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="callback"></param>
    /// <returns></returns>
    public bool SendCommandRcon(string cmd, Action<ResponsePacket?>? callback)
    {
        return _rconWorker.SendCommand(cmd, callback);
    }
    
    /// <summary>
    /// Reconnect to the WebSocket
    /// </summary>
    public void ReconnectRcon()
    {
        _rconWorker.ForceReconnect();
    }
    
    public async Task ProcessMessageDiscord(ResponseMessage message)
    {
        await _discordWorker.ProcessMessage(message);
        
        _spamHandler.RegisterMessage(message.UserID, message.Content);
    }    
    
    public async Task ProcessCommandRconCallback(ResponsePacket response)
    {
        await _discordWorker.ProcessCommandRconCallback(response, _configuration.Chatlog.ChannelId);
    }
    
    public async Task ProcessServerInfo(ResponseServerInfo response)
    {
        await _discordWorker.ProcessMessage(response);
        
        _balanceController?.AddFps(response.Framerate);
    }   
    
    public void ProcessJoin(ulong steamId)
    {
        _logger.Debug("{0} User {1} just joined.",  "[" + GetName() + "]", steamId);
        
        _discordWorker.ProcessMessage(steamId);
        Task.Run(() => _discordWorker.JDLogEntry(steamId, true));
    }    
    
    public void ProcessDisconnect(ulong steamId)
    {
        _logger.Debug("{0} User {1} just disconnected.",  "[" + GetName() + "]", steamId);

        Task.Run(() => _discordWorker.JDLogEntry(steamId, false));
    }

    public void UpdateServerWorldSeed(string seed)
    {
        _configuration.ServerInfo.WorldSeed = seed;
    }    
    
    public void UpdateServerWorldSize(string worldSize)
    {
        _configuration.ServerInfo.WorldSize = worldSize;
    }
        
    public async Task UpdateStatusDiscord(string status, bool fail = false)
    {
        await _discordWorker.SetStatus(status, fail);
    }
    
    #endregion

    #region Helpers

    public string GetName()
    {
        return _configuration.Name;
    }

    public Color GetDiscordMessageColor()
    {
        return _discordWorker.GetDiscordMessageColor();
    }

    public string GetDiscordAvatarUrl()
    {
        return _discordWorker.GetDiscordAvatarUrl();
    }
    
    public ResponseServerInfo? GetLastServerInfo()
    {
        return _discordWorker.GetLastServerInfo();
    }
    
    private async Task TryGrowth()
    {
        var delay = TimeSpan.FromSeconds(_configuration.Population.RefreshDelay);
        
        while (true)
        {
            await Task.Delay(delay);

            var lastServerInfo = GetLastServerInfo();
            if (lastServerInfo == null)
            {
                _logger.Warning("Serverinfo is null in TryGrowth() method. Skipping...");
                continue;
            }
            
            var totalPlayers = lastServerInfo.Queued + lastServerInfo.Joining + lastServerInfo.Players;
            
            int lastPresetKey = 0;
            foreach (var slotPreset in _configuration.Population.DynamicPops)
            {
                if (totalPlayers >= slotPreset.Key)
                {
                    lastPresetKey = slotPreset.Key;
                    continue;
                }
                
                break;
            }

            var resultAmount = _configuration.Population.DynamicPops[lastPresetKey];
            if (resultAmount == lastServerInfo.MaxPlayers)
                continue;
            
            SendCommandRcon(
                string.Format(_configuration.Population.RconCommand, resultAmount), response => 
                {
                    _logger.Information("Adjust pop response: {response}", response.MessageContent);
                });
        }

    }
    
    #endregion
}