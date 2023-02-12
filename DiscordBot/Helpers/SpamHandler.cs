using System;
using System.Collections.Generic;
using RustyWatcher.Controllers;
using Serilog;

namespace RustyWatcher.Helpers;

public class SpamHandler
{
    private static readonly ILogger _logger = Log.ForContext<SpamHandler>();
    
    private readonly Dictionary<ulong, MessageData> _recentUserMessages = new();
    private readonly Connector _connector;
    
    public SpamHandler(Connector connector)
    {
        _connector = connector;
    }
    
    public void RegisterMessage(ulong userId, string content)
    {
        if (userId == 0)
            return;
        
        if (!_recentUserMessages.TryGetValue(userId, out var data))
            data = _recentUserMessages[userId] = new MessageData();
        
        if (!data.IsSpamAndUpdate(content))
            return;
        
        _connector.SendCommandRcon($"mute {userId} 1h Spam", null);
        _logger.Warning("Muted player {userId} for spam.", userId);
    }
    
    private class MessageData
    {
        private static readonly TimeSpan _allowedSendFrequency = TimeSpan.FromSeconds(7);

        private DateTime _last;
        private string _lastContent;
        private int _triggerCount;
        
        public MessageData()
        {
            _lastContent = string.Empty;
            _last = DateTime.UtcNow.Subtract(_allowedSendFrequency);
            _triggerCount = 0;
        }

        public bool IsSpamAndUpdate(string content)
        {
            // Spam
            if (content == _lastContent && DateTime.UtcNow - _last < _allowedSendFrequency)
            {
                _triggerCount++;
                return _triggerCount >= 3;
            }

            _last = DateTime.UtcNow;
            _lastContent = content;
            _triggerCount = 0;
            return false;
        }
    }
}