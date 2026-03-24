using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RustyWatcher.Configurations;
using RustyWatcher.Controllers;
using RustyWatcher.Extensions;
using Serilog;

namespace RustyWatcher.Helpers;

public class SpamHandler
{
    private static readonly ILogger Logger = Log.ForContext<SpamHandler>();
    private static readonly SpamConfiguration Configuration = Configurations.Configuration.Instance.SpamConfiguration;

    private readonly ConcurrentDictionary<ulong, MessageData> _recentUserMessages = new();
    private readonly Connector _connector;
    
    public SpamHandler(Connector connector)
    {
        _connector = connector;

        _ = ExpireCycle();
    }
    
    public void RegisterMessage(ulong userId, string username, string content)
    {
        if (userId == 0)
            return;
        
        if (!_recentUserMessages.TryGetValue(userId, out var data))
            data = _recentUserMessages[userId] = new MessageData();
        
        if (!data.IsSpamAndUpdate(content))
            return;

        var cmd = Configuration.CommandFormat;
        cmd = cmd.Replace("{SteamId}", userId.ToString());
        cmd = cmd.Replace("{Username}", username);
        _connector.SendCommandRcon(cmd, null);
        
        Logger.Information("Muted player {userId} / {username} for spam.", userId, username);
    }

    private async Task ExpireCycle()
    {
        var frequency = TimeSpan.FromMinutes(1);
        var expireIds = new List<ulong>();
        
        while (true)
        {
            foreach (var (userId, messageData) in _recentUserMessages)
            {
                if (!messageData.ShouldExpire())
                    continue;
                
                expireIds.Add(userId);
            }

            if (expireIds.Count > 0)
            {
                foreach (var expireId in expireIds)
                {
                    _recentUserMessages.TryRemove(expireId, out _);
                }
                expireIds.Clear();
            }

            await Task.Delay(frequency);
        }
    }
    
    private class MessageData
    {
        private static SpamConfiguration Config => Configurations.Configuration.Instance.SpamConfiguration;

        private DateTime _last = DateTime.UtcNow.Subtract(Config.AllowedSendFrequency);
        private readonly List<string> _pastContents = new();
        private int _quickSpamTriggerCount;

        public bool ShouldExpire()
        {
            return _last + TimeSpan.FromMinutes(30) < DateTime.UtcNow;
        }
        
        public bool IsSpamAndUpdate(string content)
        {
            var prevMsgTime = _last;
            _last = DateTime.UtcNow;
            
            content = content.ToLower();
            
            var lastContent = _pastContents.Count > 1 ? _pastContents[^1] : string.Empty;
            _pastContents.Add(content);
            
            // Spam same message
            var sameContentStreak = GetSameContentStreak(content); // expected to be 1 by default
            if (sameContentStreak >= Config.ContentStreakMuteCeiling && content.Length > Config.IgnoreContentStreakLengthFloor)
                return true;
            
            // Quick spam
            if (IsSame(content, lastContent) && DateTime.UtcNow - prevMsgTime < Config.AllowedSendFrequency)
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
                if (!IsSame(content, baseContent) || Regex.IsMatch(content, Config.IgnoreContentStreakRegex))
                    break;
                
                count++;
            }

            return count;
        }

        private static bool IsSame(string input1, string input2)
        {
            if (input1.Length < Config.MinLengthForLevenshtein || input2.Length < Config.MinLengthForLevenshtein)
                return input1 == input2;

            return input1.LevenshteinDistanceRate(input2) >= Config.PercentageForLevenshteinSame;
        }
    }
}