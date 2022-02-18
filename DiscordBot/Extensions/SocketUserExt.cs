using System.Collections.Generic;
using Discord;
using Discord.WebSocket;

namespace RustyWatcher.Extensions;

public static class SocketUserExt
{
    public static bool HasAnyRole(this SocketUser user, List<ulong> roleIds)
    {
        var guildUser = user as IGuildUser;
        if (guildUser == null)
            return false;
        
        foreach (var roleId in guildUser.RoleIds)
        {
            if (!roleIds.Contains(roleId))
                continue;

            return true;
        }

        return false;
    }
}