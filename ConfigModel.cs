using System;
using System.Collections.Generic;
using System.Text;

namespace VoiceOnlineBot
{
    public class ConfigModel
    {
        public string BotToken;
        public ulong[] OwnerIds;
        public ulong[] LikeCheckOwnersIds;
        public ulong GuildID;
        public string NadekoDBPath;
        public ulong AnonChannelID;
        public string PrizeRoleName;
        public int DragonCooldown;
    }
}
