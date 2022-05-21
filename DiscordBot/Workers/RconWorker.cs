using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RustyWatcher.Configurations;
using RustyWatcher.Controllers;
using RustyWatcher.Extensions;
using RustyWatcher.Models.Rcon;
using Serilog;
using WebSocketSharp;

namespace RustyWatcher.Workers;

public class RconWorker
{
    #region Fields
    
    private static readonly ILogger _logger = Log.ForContext<RconWorker>();

    private readonly Connector _connector;
    private readonly RconConfiguration _configuration;

    private readonly WebSocket _webSocket;
    private bool _isConnected => _webSocket.ReadyState == WebSocketState.Open && _webSocket.ReadyState != WebSocketState.Closed;
    
    private readonly Dictionary<int, Action<ResponsePacket?>?> _awaitingCallback = new();
    private int _currentIdentifier = 1337;

    private const string REGEX_MATCH_JOINED = @".+?joined \[.+?(?=\/)\/[0-9]{17}\]$";
    private const string REGEX_MATCH_JOINED_OPTIONAL = @".+?steamid [0-9]{17} joined";
    private const string REGEX_MATCH_DISCONNECT = @"^[0-9]{17}.+?(?=disconnecting:).+";
    
    private const string REGEX_MATCH_STEAMID = @"[0-9]{17}";

    #endregion
    
    public RconWorker(Connector connector, RconConfiguration configuration)
    {
        _connector = connector;
        _configuration = configuration;
        
        _webSocket = new WebSocket($"ws://{configuration.ServerIP}:{configuration.RconPort}/{configuration.RconPW}");
        
        Task.Run(InitializeWebSocket);
        Task.Run(PollServerInfo);
    }

    #region Init

    private async Task InitializeWebSocket()
    {
        _webSocket.Log.Output = (_, __) => { };

        _logger.Information("{0} Connecting...", GetTag());

        try
        {
            _webSocket.OnOpen += OnOpen;
            _webSocket.OnClose += OnClose;
            _webSocket.OnMessage += OnMessage;
            _webSocket.OnError += OnError;

            _webSocket.Connect();
        }
        catch (Exception e)
        {
            _logger.Error(e, "{0} Failed to Initialize WebSocket", GetTag());
        } 

        await Task.Delay(-1);
    }
    
    #endregion

    #region Events

    private void OnError(object? sender, ErrorEventArgs e)
    {
        _logger.Error(e.Exception, "{0} RconWorker.OnError\n" + e.Message, GetTag());
    }

    private void OnMessage(object? sender, MessageEventArgs e)
    {
        Task.Run(() => ProcessMessage(e));
    }

    private void OnClose(object? sender, CloseEventArgs e)
    {
        _logger.Information("{0} Lost Connection!", GetTag());
        Task.Run(() => _connector.UpdateStatusDiscord("Offline", true));
        Task.Run(TryReconnect);
    }

    private void OnOpen(object? sender, EventArgs e)
    {
        _logger.Information("{0} Connected!", GetTag());
        Task.Run(() => _connector.UpdateStatusDiscord("Connecting..."));

        GetMapSeed();
        GetMapSize();
    }

    #endregion
    
    #region Methods

    private async Task ProcessMessage(MessageEventArgs args)
    {
        try
        {
            if (!args.Data.TryParseJson(out ResponsePacket? result))
                return;
            
            if (result == null)
                return;
            
            if (_awaitingCallback.TryGetValue(result.Identifier, out var responseAction) && !string.IsNullOrEmpty(result.MessageContent))
            {
                responseAction?.Invoke(result);
                Log.Debug("{tag} New Callback Message with Identifier: {identifier}\nMessage: {message}", GetTag(), result.Identifier, result.MessageContent);
                _awaitingCallback.Remove(result.Identifier);
                return;
            }
            
            //Log.Debug("{0} New Message with Identifier: " + result.Identifier, GetTag());

            switch (result.Identifier)
            {
                case (int)PacketIdentifier.ServerInfo:
                {
                    if (!result.MessageContent.TryParseJson<ResponseServerInfo>(out var serverInfo) || serverInfo == null)
                        return;
                    
                    await _connector.ProcessServerInfo(serverInfo);
                    break;
                }
                case (int)PacketIdentifier.WorldSeed:
                {
                    _connector.UpdateServerWorldSeed(Regex.Replace(
                        Regex.Replace(result.MessageContent, "\\W", string.Empty), "[a-zA-Z]", string.Empty));
                        break;
                }                    
                case (int)PacketIdentifier.WorldSize:
                {
                    _connector.UpdateServerWorldSize(Regex.Replace(
                        Regex.Replace(result.MessageContent, "\\W", string.Empty), "[a-zA-Z]", string.Empty));
                        break;
                }
                case (int)PacketIdentifier.DiscordCommand:
                {
                    await _connector.ProcessCommandRconCallback(result);
                    break;
                }
                default:
                {
                    if (!result.MessageContent.TryParseJson<ResponseMessage>(out var message))
                    {
                        if (Regex.IsMatch(result.MessageContent, REGEX_MATCH_JOINED))
                        {
                            var steamId = Regex.Match(result.MessageContent, REGEX_MATCH_STEAMID).Value;
                            _connector.ProcessJoin(ulong.Parse(steamId));
                        }
                        else if (Regex.IsMatch(result.MessageContent, REGEX_MATCH_JOINED_OPTIONAL)) 
                        {
                            // if new player basically, otherwise above gets called
                            var steamId = Regex.Match(result.MessageContent, REGEX_MATCH_STEAMID).Value;
                            _connector.ProcessJoin(ulong.Parse(steamId));
                        }                        
                        else if (Regex.IsMatch(result.MessageContent, REGEX_MATCH_DISCONNECT)) 
                        {
                            var steamId = Regex.Match(result.MessageContent, REGEX_MATCH_STEAMID).Value;
                            _connector.ProcessDisconnect(ulong.Parse(steamId));
                        }

                        return;
                    }
                    
                    if (message == null || message.Content.IsNullOrEmpty())
                        return;
                    
                    await _connector.ProcessMessageDiscord(message);
                    break;
                }
            }                              
        }
        catch (Exception e)
        {
            _logger.Error(e, "{0} Failed to Process Message", GetTag());
        }
    }
    
    public bool SendCommand(string cmd, Action<ResponsePacket?>? callback)
    {
        _currentIdentifier++;
        
        Log.Debug("Sending Command with Identifier {identifier}\nCommand: {command}", _currentIdentifier, cmd);
        
        _awaitingCallback.Add(_currentIdentifier, (response) =>
        {
            callback?.Invoke(response);
        });

        Task.Run(() => TriggerTimeoutAsync(_currentIdentifier));
        
        return SendMessage(cmd, _currentIdentifier);
    }
    
    public bool SendMessage(string message, string username, string color, ulong steamId)
    {
        var packetMessage = new ResponseMessage
        {
            Channel = 0,
            Content = message,
            Username = username,
            UserID = steamId,
            Color = color,
            Time = (uint)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds
        };

        message = "discordsay  " + JsonConvert.SerializeObject(packetMessage);
        return SendMessage(message, (int)PacketIdentifier.DiscordMessage);
    }

    private bool SendMessage(string message, int identifier)
    {
        var packet = new Packet(message, identifier);
        var msg = JsonConvert.SerializeObject(packet);

        if (!_isConnected)
        {
            _logger.Debug("{0} Trying to send message but websocket not connected!", GetTag());
            return false;
        }
                
        _webSocket.Send(msg);
        return true;
    }

    private async Task TriggerTimeoutAsync(int currentIdentifier)
    {
        await Task.Delay(_configuration.TimeoutCommands * 1000);

        if (!_awaitingCallback.TryGetValue(currentIdentifier, out var responseAction))
            return;
        
        responseAction?.Invoke(null);
        _awaitingCallback.Remove(currentIdentifier);
    }
    
    #endregion

    #region Helpers
    
    private string GetTag()
    {
        return "[" + _connector.GetName() + "-RCON]";
    }
    
    private void GetMapSize() =>
        SendMessage("worldsize", (int)PacketIdentifier.WorldSize);
                
    private void GetMapSeed() =>
        SendMessage("seed", (int)PacketIdentifier.WorldSeed);
    
    private async Task PollServerInfo()
    {
        while (true)
        {
            if (_isConnected) 
                SendMessage("serverinfo", (int)PacketIdentifier.ServerInfo);
            else 
                await _connector.UpdateStatusDiscord("Offline", true);

            await Task.Delay(Configuration.Instance.UpdateDelay * 1000);
        }
    }
    
    private async Task TryReconnect()
    {
        if (!_isConnected)
        {
            await Task.Delay(_configuration.ReconnectDelay * 1000);
            _logger.Information("{0} Reconnecting...", GetTag());
            
            try
            {
                if (!_isConnected) 
                    _webSocket.Connect(); 
            }
            catch { }
        }
    }

    public void ForceReconnect()
    {
        if (_isConnected)
            _webSocket.Close(); // results in TryReconnect call
        else
            Task.Run(TryReconnect);
    }

    #endregion
}