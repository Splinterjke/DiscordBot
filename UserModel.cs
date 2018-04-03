using System;
using System.Collections.Generic;
using System.Text;

namespace VoiceOnlineBot
{
	public class UserModel
	{
		public ulong Id { get; set; }
		public string Username { get; set; }
		public bool IsConnected { get; set; }
		public ulong PartnerId { get; set; }
		public DateTimeOffset? LastConnectedTime { get; set; }
		public DateTimeOffset? LastUpdateTime { get; set; }
		public TimeSpan? LastSessionTime { get; set; }
		public TimeSpan VoiceOnlineTime { get; set; }
		public uint GiftCount { get; set; }
		public uint PistolCount { get; set; }
		public uint DragonEggCount { get; set; }
		public uint IncubatorCount { get; set; }
		public uint DragonCount { get; set; }
		public uint PaintCount { get; set; }
		public uint AmmoCount { get; set; }
		public uint RingCount { get; set; }
		public bool Hours10Prize { get; set; }
		public bool Hours20Prize { get; set; }
		public bool Hours40Prize { get; set; }
		public bool Hours60Prize { get; set; }
		public bool Hours90Prize { get; set; }
		public bool Hours120Prize { get; set; }
		public bool Hours150Prize { get; set; }
		public DateTimeOffset? LastPackUsedTime { get; set; }
		public List<DateTimeOffset> EggRespawnTimes { get; set; }
		public List<DateTimeOffset> IncubatorRespawnTimes { get; set; }
		public DateTimeOffset? SmallShieldEndTime { get; set; }
		public uint SuperShieldEndurance { get; set; }
		public DateTimeOffset? LastAssassinatedTime { get; set; }
		public DateTimeOffset? MarriageTime { get; set; }
	}
}
