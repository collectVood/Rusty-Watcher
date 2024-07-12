using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RustyWatcher.Configurations;
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
        private static SpamConfiguration _config => Configuration.Instance.SpamConfiguration;

        private DateTime _last = DateTime.UtcNow.Subtract(_config.AllowedSendFrequency);
        private readonly List<string> _pastContents = new();
        private int _quickSpamTriggerCount;

        public bool IsSpamAndUpdate(string content)
        {
            var prevMsgTime = _last;
            _last = DateTime.UtcNow;
            
            content = content.ToLower();
            
            var lastContent = _pastContents.Count > 1 ? _pastContents[_pastContents.Count - 1] : string.Empty;
            _pastContents.Add(content);
            
            // Spam same message
            var sameContentStreak = GetSameContentStreak(content); // expected to be 1 by default
            if (sameContentStreak >= _config.ContentStreakMuteCeiling && content.Length > _config.IgnoreContentStreakLengthFloor)
                return true;
            
            // Quick spam
            if (content == lastContent && DateTime.UtcNow - prevMsgTime < _config.AllowedSendFrequency)
            {
                _quickSpamTriggerCount++;
                return _quickSpamTriggerCount >= 3;
            }
            
            _quickSpamTriggerCount = 0;
            return false;
        }

        private int GetSameContentStreak(string baseContent)
        {
            var count = 0;
            for (var i = _pastContents.Count - 1; i >= 0; i--)
            {
                var content = _pastContents[i];
                if (content != baseContent || Regex.IsMatch(content, _config.IgnoreContentStreakRegex))
                    break;
                
                count++;
            }

            return count;
        }
    }
}