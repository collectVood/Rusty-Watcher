using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RustyWatcher.Configurations;
using RustyWatcher.Models.Rcon;
using RustyWatcher.Workers;
using Serilog;

namespace RustyWatcher.Controllers;

public class Connector
{
    #region Fields

    private static readonly ILogger _logger = Log.ForContext<Connector>();
    private static readonly List<Connector> _connectors = new();

    private readonly ServerConfiguration _configuration;
    private readonly DiscordWorker _discordWorker;
    private readonly RconWorker _rconWorker;

    #endregion
    
    private Connector(ServerConfiguration serverConfiguration)
    {
        _configuration = serverConfiguration;
        
        _discordWorker = new DiscordWorker(this, serverConfiguration);
        _rconWorker = new RconWorker(this, serverConfiguration.Rcon);
    }

    #region Init

    public static void StartAll()
    {
        Log.Information("Starting Connectors...");
        
        foreach (var serverConfiguration in Configuration.Instance.Servers)
            _connectors.Add(new Connector(serverConfiguration));
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
    /// <param name="channelId"></param>
    /// <returns></returns>
    public bool SendCommandRcon(string cmd, Action<ResponsePacket>? callback)
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
    }    
    
    public async Task ProcessCommandRconCallback(ResponsePacket response)
    {
        await _discordWorker.ProcessCommandRconCallback(response, _configuration.Chatlog.ChannelId);
    }
    
    public async Task ProcessServerInfo(ResponseServerInfo response)
    {
        await _discordWorker.ProcessMessage(response);
    }   
    
    public void ProcessJoin(ulong steamId)
    {
        _logger.Debug("{0} User {1} just joined.", GetName(), steamId);
        
        _discordWorker.ProcessMessage(steamId);
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
    
    #endregion
}