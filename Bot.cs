using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LiteDB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace VoiceOnlineBot
{
	public class Bot
	{
		//"NDE3MDM4NjQ5Mjg1Mjc5NzQ1.DXNNDw.6IlC6SMN9gAqllQzRVe8IqndBOY";
		public static ConfigModel botConfig;
		private bool TasksIsRunning;
		private bool IsConnected;
		public static Random random = new Random();
		private DiscordClient client;
		private CommandsNextModule cnext;
		private HashSet<ulong> roleMessages = new HashSet<ulong>();
		private List<DiscordRole> guildRoles;
		private List<DiscordDmChannel> ownersChannels;
		private DiscordChannel targetChannel;
		public static DiscordGuild targetGuild;
		public static DiscordRole prizeRole;
		private IReadOnlyCollection<string> badWords;
		private DiscordChannel dragonCategory, dragonGiftsChannel;
		public static DateTimeOffset nextDragonFly;
		private Dictionary<string, string> Replacements = new Dictionary<string, string>()
		{ { "a", "а" }, { "b", "б" }, { "v", "в" }, { "g", "г" }, { "d", "д" }, { "e", "е" }, { "yo", "ё" }, { "zh", "ж" }, { "z", "з" }, { "i", "и" }, { "k", "к" }, { "l", "л" }, { "m", "м" }, { "n", "н" }, { "o", "о" }, { "p", "п" }, { "r", "р" }, { "s", "с" }, { "t", "т" }, { "u", "у" }, { "f", "ф" }, { "h", "х" }, { "ts", "ц" }, { "ch", "ч" }, { "sh", "ш" }, { "sch", "щ" }, { "yi", "ы" }, { "'", "ь" }, { "yu", "ю" }, { "ya", "я" }
		};
		private static object locker = new object();
		public async Task Run()
		{
			try
			{
				if (File.Exists("badwords.json"))
				{
					using (var sr = new StreamReader("badwords.json"))
					{
						string jsonString = string.Empty;
						string line;
						while ((line = sr.ReadLine()) != null)
						{
							jsonString += line;
						}
						badWords = JsonConvert.DeserializeObject<IReadOnlyCollection<string>>(jsonString);
					}
				}
				else
					throw new Exception("badwords.json file is not found");

				if (File.Exists("config.json"))
				{
					using (var sr = new StreamReader("config.json"))
					{
						string jsonString = string.Empty;
						string line;
						while ((line = sr.ReadLine()) != null)
						{
							jsonString += line;
						}
						botConfig = JsonConvert.DeserializeObject<ConfigModel>(jsonString);
						if (string.IsNullOrEmpty(botConfig.BotToken))
							throw new Exception("BotToken is empty");
					}
				}
				else
					throw new Exception("config.json file is not found");
				client = new DiscordClient(new DiscordConfiguration()
				{
					AutoReconnect = true,
					Token = botConfig.BotToken,
					TokenType = TokenType.Bot,
					UseInternalLogHandler = false
				});
				cnext = client.UseCommandsNext(new CommandsNextConfiguration()
				{
					CaseSensitive = false,
					EnableDefaultHelp = false,
					EnableDms = false,
					EnableMentionPrefix = false,
					StringPrefix = "!",
					IgnoreExtraArguments = true
				});

				cnext.RegisterCommands<Commands>();
				cnext.CommandErrored += Cnext_CommandErrored;
				client.Ready += Client_Ready;
				client.SocketClosed += Client_SocketClosed;
				client.ClientErrored += Client_ClientError;
				client.SocketOpened += Client_SocketOpened;
				client.GuildAvailable += Client_GuildAvailable;
				client.VoiceStateUpdated += Client_VoiceStateUpdated;
				client.MessageReactionAdded += Client_MessageReactionAdded;
				client.MessageReactionRemoved += Client_MessageReactionRemoved;
				client.DebugLogger.LogMessageReceived += DebugLogger_LogMessageReceived;
				client.MessageCreated += Client_MessageCreated;

				Log.Logger.Information($"Starting bot client");
				await client.ConnectAsync();
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
			}
			await Task.Delay(-1);
		}

		private Task Client_SocketOpened()
		{
			IsConnected = true;
			return Task.CompletedTask;
		}

		private Task Client_SocketClosed(SocketCloseEventArgs e)
		{
			IsConnected = false;
			return Task.CompletedTask;
		}

		private void DebugLogger_LogMessageReceived(object sender, DebugLogMessageEventArgs e)
		{
			Log.Logger.Debug(e.Message);
		}

		public class MessageReactionEventArgs
		{
			public MessageReactionEventArgs(MessageReactionRemoveEventArgs e)
			{
				Message = e.Message;
				Channel = e.Channel;
				User = e.User;
				Emoji = e.Emoji;
			}

			public MessageReactionEventArgs(MessageReactionAddEventArgs e)
			{
				Message = e.Message;
				Channel = e.Channel;
				User = e.User;
				Emoji = e.Emoji;
			}

			public DiscordMessage Message { get; set; }
			public DiscordChannel Channel { get; set; }
			public DiscordUser User { get; set; }
			public DiscordEmoji Emoji { get; set; }
		}

		private Task Client_MessageReactionRemoved(MessageReactionRemoveEventArgs e)
		{
			ProcessReactions(new MessageReactionEventArgs(e));
			return Task.CompletedTask;
		}

		private Task Client_MessageReactionAdded(MessageReactionAddEventArgs e)
		{
			ProcessReactions(new MessageReactionEventArgs(e));
			return Task.CompletedTask;
		}

		private async void ProcessReactions(MessageReactionEventArgs e)
		{
			try
			{
				if (roleMessages != null && roleMessages.Contains(e.Message.Id) && !e.User.IsBot)
				{
					var user = e.User as DiscordMember;
					string roleName = default;
					var emojiName = e.Emoji.GetDiscordName();
					switch (emojiName)
					{
						case ":pis:":
							roleName = "CS:GO";
							break;
						case ":dota:":
							roleName = "Dota 2";
							break;
						case ":regional_indicator_m:":
							roleName = "MMORPG";
							break;
						case ":pabg:":
							roleName = "PUBG";
							break;
						case ":gta5:":
							roleName = "GTAV";
							break;
						case ":lol:":
							roleName = "LoL";
							break;
						case ":hs:":
							roleName = "Hearthstone";
							break;
						case ":osu:":
							roleName = "osu!";
							break;
						case ":bullet:":
							roleName = "WoT";
							break;
						case ":gem:":
							roleName = "Paladins";
							break;
						case ":owe:":
							roleName = "Overwatch";
							break;
						case ":🤡:":
							roleName = "Dead by Daylight";
							break;
						case ":wrc:":
							roleName = "Warcraft 3";
							break;
						case ":spy:":
							roleName = "уч. Мафии";
							break;
						case ":min:":
							roleName = "Minecraft";
							break;
						case ":syringe:":
							roleName = "left4dead";
							break;
						default:
							return;
					}
					var role = user.Roles.FirstOrDefault(x => x.Name == roleName);
					if (role != null)
					{
						await user.RevokeRoleAsync(role);
					}
					else
					{
						await user.GrantRoleAsync(guildRoles.Find(x => x.Name == roleName));
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
			}
			return;
		}

		private async Task Cnext_CommandErrored(CommandErrorEventArgs e)
		{
			if (e.Command != null && e.Command.ExecutionChecks != null && e.Command.ExecutionChecks.Count > 0 && e.Command.ExecutionChecks[0] is CooldownAttribute)
			{
				await e.Context.RespondAsync($"Команда !{e.Command.Aliases[0]} будет доступна через: {((e.Command.ExecutionChecks[0] as CooldownAttribute).GetBucket(e.Context).ResetsAt - DateTimeOffset.UtcNow).Seconds} сек.");
				return;
			}
			if (e.Exception.Message == "Could not convert specified value to given type.\r\nParameter name: value")
			{
				var embed = new DiscordEmbedBuilder
				{
					Description = "Некорректно указан порядок или тип аргументов"
				};
				await e.Context.RespondAsync(embed: embed);
				return;
			}
			if (e.Exception.Message == "Specified command was not found.") return;
			Log.Logger.Error(e.Exception.ToString());
		}

		private async Task Client_GuildAvailable(GuildCreateEventArgs e)
		{
			if (TasksIsRunning) return;
			try
			{
				if (e.Guild.Id == botConfig.GuildID)
				{
					Log.Logger.Information($"Handling tasks for {e.Guild.Name} guild");
					dragonCategory = e.Guild.Channels.FirstOrDefault(x => x.Id == 408807213553942550);
					dragonGiftsChannel = e.Guild.Channels.FirstOrDefault(x => x.Id == 408802925289406475);
					if (botConfig.NadekoDBPath == null || !File.Exists(botConfig.NadekoDBPath))
						throw new Exception("Nadeko database file is not found");
					prizeRole = e.Guild.Roles.FirstOrDefault(x => x.Name == botConfig.PrizeRoleName);
					if (prizeRole == null)
						throw new Exception("Prize role is not found");
					targetGuild = e.Guild;
					var members = targetGuild.Members.Where(x => !x.IsBot).ToArray();
					using (var db = new LiteDatabase($"filename={botConfig.GuildID}_voiceStat.db; journal=false;"))
					{
						var newUsers = new List<UserModel>();
						var currentUsers = ImmutableList.CreateBuilder<UserModel>();
						var col = db.GetCollection<UserModel>("Users");
						col.EnsureIndex(x => x.Id, true);
						for (int i = 0; i < members.Length; i++)
						{
							try
							{
								var dbUser = col.FindOne(x => x.Id == members[i].Id);
								if (dbUser != null)
								{
									if (dbUser.EggRespawnTimes != null && dbUser.EggRespawnTimes.Count >= int.MaxValue - 100)
									{
										dbUser.EggRespawnTimes.RemoveRange(1, dbUser.EggRespawnTimes.Count - 2);
										col.Update(dbUser);
									}

									if (dbUser.IncubatorRespawnTimes != null && dbUser.IncubatorRespawnTimes.Count >= int.MaxValue - 100)
									{
										dbUser.IncubatorRespawnTimes.RemoveRange(1, dbUser.IncubatorRespawnTimes.Count - 2);
										col.Update(dbUser);
									}

									if (dbUser.DragonCount > uint.MaxValue - 100)
									{
										dbUser.DragonCount = uint.MaxValue - dbUser.DragonCount;
										col.Update(dbUser);
									}
								}
								if (dbUser == null)
								{
									var userData = new UserModel
									{
										Id = members[i].Id,
										Username = members[i].Username,
										IsConnected = members[i].VoiceState != null && members[i].VoiceState.Channel != null,
										LastConnectedTime = members[i].VoiceState != null && members[i].VoiceState.Channel != null ? (DateTimeOffset?)DateTimeOffset.UtcNow : null,
										LastUpdateTime = members[i].VoiceState != null && members[i].VoiceState.Channel != null ? (DateTimeOffset?)DateTimeOffset.UtcNow : null,
										VoiceOnlineTime = TimeSpan.Zero
									};
									newUsers.Add(userData);
								}
								else
								if (members[i].VoiceState != null && members[i].VoiceState.Channel != null)
								{
									if (!dbUser.IsConnected)
									{
										dbUser.IsConnected = true;
										dbUser.Username = members[i].Username;
										dbUser.LastConnectedTime = DateTimeOffset.UtcNow;
										dbUser.LastUpdateTime = DateTimeOffset.UtcNow;
										currentUsers.Add(dbUser);
									}
								}
								else
								{
									dbUser.Username = members[i].Username;
									dbUser.IsConnected = false;
									dbUser.LastConnectedTime = null;
									dbUser.LastUpdateTime = DateTimeOffset.UtcNow;
									currentUsers.Add(dbUser);
								}
							}
							catch (Exception ex)
							{
								Log.Error(ex.ToString());
								i++;
							}
						}
						col.InsertBulk(newUsers);
						col.Update(currentUsers);
					}
					UpdateUsersOnlineStat();
					DragonGift();
					DragonBorn();
					TasksIsRunning = true;
					guildRoles = e.Guild.Roles.Where(x => x.Name == "Dota 2" || x.Name == "CS:GO" || x.Name == "MMORPG" || x.Name == "PUBG" || x.Name == "GTAV" || x.Name == "osu!" || x.Name == "WoT" || x.Name == "Hearthstone" || x.Name == "Paladins" || x.Name == "Overwatch" || x.Name == "LoL" || x.Name == "Dead by Daylight" || x.Name == "уч. Мафии" || x.Name == "Minecraft" || x.Name == "left4dead" || x.Name == "Warcraft 3").ToList();
					await Task.Delay(3000);
					SendRoleChooser(415444284305702912);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
				await client.DisconnectAsync();
			}
		}

		private async void SendRoleChooser(ulong channelId)
		{
			var rolesChannel = await client.GetChannelAsync(channelId);
			var messages = await rolesChannel.GetMessagesAsync();
			var roleMessage = messages.FirstOrDefault(x => x.Author.Id == client.CurrentUser.Id && x.Content.Contains("выбрать или убрать роль"));
			if (roleMessage != null)
			{
				roleMessages.Add(roleMessage.Id);
				return;
			}
			roleMessage = await rolesChannel.SendMessageAsync("Вы можете выбрать или убрать роль, кликнув по соответствующей реакции");
			roleMessages.Add(roleMessage.Id);
			await roleMessage.CreateReactionAsync(DiscordEmoji.FromName(client, ":osu:"));
			await Task.Delay(1000);
			await roleMessage.CreateReactionAsync(DiscordEmoji.FromName(client, ":lol:"));
			await Task.Delay(1000);
			await roleMessage.CreateReactionAsync(DiscordEmoji.FromName(client, ":bullet:"));
			await Task.Delay(1000);
			await roleMessage.CreateReactionAsync(DiscordEmoji.FromName(client, ":pabg:"));
			await Task.Delay(1000);
			await roleMessage.CreateReactionAsync(DiscordEmoji.FromName(client, ":gta5:"));
			await Task.Delay(1000);
			await roleMessage.CreateReactionAsync(DiscordEmoji.FromName(client, ":pis:"));
			await Task.Delay(1000);
			await roleMessage.CreateReactionAsync(DiscordEmoji.FromName(client, ":dota:"));
			await Task.Delay(1000);
			await roleMessage.CreateReactionAsync(DiscordEmoji.FromName(client, ":gem:"));
			await Task.Delay(1000);
			await roleMessage.CreateReactionAsync(DiscordEmoji.FromName(client, ":syringe:"));
			await Task.Delay(1000);
			await roleMessage.CreateReactionAsync(DiscordEmoji.FromName(client, ":min:"));
			await Task.Delay(1000);
			await roleMessage.CreateReactionAsync(DiscordEmoji.FromName(client, ":regional_indicator_m:"));
			await Task.Delay(1000);
			await roleMessage.CreateReactionAsync(DiscordEmoji.FromName(client, ":owe:"));
			await Task.Delay(1000);
			await roleMessage.CreateReactionAsync(DiscordEmoji.FromName(client, ":spy:"));
			await Task.Delay(1000);
			await roleMessage.CreateReactionAsync(DiscordEmoji.FromName(client, ":wrc:"));
			await Task.Delay(1000);
			await roleMessage.CreateReactionAsync(DiscordEmoji.FromName(client, ":hs:"));
			await Task.Delay(1000);
			await roleMessage.CreateReactionAsync(DiscordEmoji.FromUnicode(client, "🤡"));
		}

		private async Task<UserModel> SendTimeGifts(UserModel dbUser)
		{
			try
			{
				var currentGiftCount = dbUser.GiftCount;
				if (dbUser.VoiceOnlineTime.TotalHours >= 10 && dbUser.VoiceOnlineTime.TotalHours < 20 && !dbUser.Hours10Prize)
				{
					dbUser.Hours10Prize = true;
					dbUser.GiftCount++;
				}
				if (dbUser.VoiceOnlineTime.TotalHours >= 20 && dbUser.VoiceOnlineTime.TotalHours < 40 && !dbUser.Hours20Prize)
				{
					dbUser.Hours20Prize = true;
					dbUser.GiftCount++;
				}
				if (dbUser.VoiceOnlineTime.TotalHours >= 40 && dbUser.VoiceOnlineTime.TotalHours < 60 && !dbUser.Hours40Prize)
				{
					dbUser.Hours40Prize = true;
					dbUser.GiftCount++;
				}
				if (dbUser.VoiceOnlineTime.TotalHours >= 60 && dbUser.VoiceOnlineTime.TotalHours < 90 && !dbUser.Hours60Prize)
				{
					dbUser.Hours60Prize = true;
					dbUser.GiftCount++;
				}
				if (dbUser.VoiceOnlineTime.TotalHours >= 90 && dbUser.VoiceOnlineTime.TotalHours < 120 && !dbUser.Hours90Prize)
				{
					dbUser.Hours90Prize = true;
					dbUser.GiftCount += 2;
				}
				if (dbUser.VoiceOnlineTime.TotalHours >= 120 && dbUser.VoiceOnlineTime.TotalHours < 150 && !dbUser.Hours120Prize)
				{
					dbUser.Hours120Prize = true;
					dbUser.GiftCount += 2;
				}
				if (dbUser.VoiceOnlineTime.TotalHours >= 150 && !dbUser.Hours150Prize)
				{
					dbUser.Hours150Prize = true;
					dbUser.GiftCount += 3;
				}
				if (dbUser.GiftCount > currentGiftCount)
				{
					var embed = new DiscordEmbedBuilder
					{
						Author = new DiscordEmbedBuilder.EmbedAuthor
						{
							Name = "🎁 Награда",
						},
						Description = $"<@{dbUser.Id}> Вы получили {dbUser.GiftCount - currentGiftCount} 🎁 за {(int)(Math.Floor(dbUser.VoiceOnlineTime.TotalHours / 10.0d) * 10)} ч. голосового онлайна",
						Color = new DiscordColor("#329EFB"),
						Timestamp = DateTime.Now,
						Footer = new DiscordEmbedBuilder.EmbedFooter
						{
							Text = "http://194.67.207.194",
						}
					};
					var msgChannel = await client.GetChannelAsync(426193823492997121);
					if (msgChannel == null)
						return dbUser;
					await msgChannel.SendMessageAsync(embed: embed);
				}
				return dbUser;
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
				return dbUser;
			}
		}

		private async Task Client_VoiceStateUpdated(VoiceStateUpdateEventArgs e)
		{
			try
			{
				if (e.Guild.Id == botConfig.GuildID && !e.User.IsBot)
				{
					targetGuild = e.Guild;
					var currentUser = e.User as DiscordMember;
					using (var db = new LiteDatabase($"filename={botConfig.GuildID}_voiceStat.db; journal=false;"))
					{
						var col = db.GetCollection<UserModel>("Users");
						col.EnsureIndex(x => x.Id, true);
						var dbUser = col.FindOne(x => x.Id == currentUser.Id);
						if (dbUser == null)
						{
							var userData = new UserModel
							{
								Id = currentUser.Id,
								Username = currentUser.Username,
								IsConnected = currentUser.VoiceState != null && currentUser.VoiceState.Channel != null,
								LastConnectedTime = currentUser.VoiceState != null && currentUser.VoiceState.Channel != null ? (DateTimeOffset?)DateTimeOffset.UtcNow : null,
								LastUpdateTime = currentUser.VoiceState != null && currentUser.VoiceState.Channel != null ? (DateTimeOffset?)DateTimeOffset.UtcNow : null,
								VoiceOnlineTime = TimeSpan.Zero
							};
							col.Insert(userData);
							dbUser = col.FindOne(x => x.Id == currentUser.Id);
						}
						dbUser = await SendTimeGifts(dbUser);
						if (e.Channel != null)
						{
							if (!dbUser.IsConnected)
							{
								dbUser.Username = currentUser.Username;
								dbUser.IsConnected = true;
								dbUser.LastConnectedTime = DateTimeOffset.UtcNow;
								dbUser.LastUpdateTime = DateTimeOffset.UtcNow;
								col.Update(dbUser);
							}
						}
						else
						{
							dbUser.Username = currentUser.Username;
							dbUser.IsConnected = false;
							if (dbUser.LastConnectedTime != null)
							{
								dbUser.LastSessionTime = DateTimeOffset.UtcNow - dbUser.LastConnectedTime.Value;
								dbUser.VoiceOnlineTime += DateTimeOffset.UtcNow - dbUser.LastUpdateTime.Value;
								dbUser.LastUpdateTime = DateTimeOffset.UtcNow;
								dbUser.LastConnectedTime = null;
							}
							col.Update(dbUser);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
			}
		}

		private string TranslitFileName(string source)
		{
			var result = "";
			foreach (var ch in source)
			{
				if (Replacements.TryGetValue(ch.ToString(), out string ss))
				{
					result += ss;
				}
				else result += ch;
			}
			return result;
		}

		private async Task Client_MessageCreated(MessageCreateEventArgs e)
		{
			try
			{
				if (badWords != null && badWords.Count > 0 && !e.Author.IsBot && e.Author.Id != 149834096401448960 && e.Author.Id != 362869994662395914 && !e.Message.Content.StartsWith(";;") && !e.Message.Content.StartsWith("!!"))
				{
					var message = TranslitFileName(e.Message.Content.ToLower());
					var messageBadWords = badWords.Where(x => message.Contains(x)).ToList();
					if (messageBadWords.Count == 0) return;
					if (!e.Channel.IsPrivate)
						await e.Message.DeleteAsync();
					var dmChannel = await client.CreateDmAsync(e.Author);
					await dmChannel.SendMessageAsync($"Ваше сообщение \"{e.Message.Content}\" содержит запрещенные слова: {String.Join(",", messageBadWords)}");
					return;
				}

				if (!e.Channel.IsPrivate || e.Author.Id == client.CurrentUser.Id || e.Message.Content.StartsWith('!') || new string[] { "https://discord", "discord.gg" }.Any(e.Message.Content.Contains))
					return;
				targetChannel = await client.GetChannelAsync(botConfig.AnonChannelID);
				if (targetChannel == null)
					throw new Exception("Couldn't get Anon channel");
				await e.Channel.TriggerTypingAsync();
				var embed = new DiscordEmbedBuilder
				{
					Author = new DiscordEmbedBuilder.EmbedAuthor
					{
						Name = "Анонимное сообщение",
						IconUrl = "http://www.unixstickers.com/image/cache/data/stickers/guyfawkes/guyfawkes.sh-600x600.png"
					},
					Description = e.Message.Content,
					Color = new DiscordColor("#329EFB"),
					Timestamp = DateTime.Now
				};
				await e.Channel.SendMessageAsync("Ваше анонимное сообщение успешно отправлено, отправитель будет скрыт.");
				await targetChannel.SendMessageAsync(embed: embed);
				for (int i = 0; i < ownersChannels.Count; i++)
				{
					await ownersChannels[i].SendMessageAsync($"Отправитель: {e.Author.Username}\nСообщение: {e.Message.Content}");
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
			}
		}

		private Task Client_ClientError(ClientErrorEventArgs e)
		{
			Log.Logger.Error(e.Exception.ToString());
			return Task.CompletedTask;
		}

		private async Task Client_Ready(ReadyEventArgs e)
		{
			try
			{
				Log.Logger.Information("Client is ready to process events.");
				IsConnected = true;
				if (TasksIsRunning) return;
				await Task.Delay(1000);
				ownersChannels = new List<DiscordDmChannel>(2);
				for (int i = 0; i < botConfig.OwnerIds.Length; i++)
				{
					var ownerUser = await client.GetUserAsync(botConfig.OwnerIds[i]);
					if (ownerUser == null)
						continue;
					var ownerChannel = await client.CreateDmAsync(ownerUser);
					ownersChannels.Add(ownerChannel);
				}
				Log.Logger.Information($"Connection with {ownersChannels.Count} owners has been established");
			}
			catch (Exception ex)
			{

				Log.Error(ex.ToString());
			}
		}

		private async void UpdateUsersOnlineStat()
		{
			while (true)
			{
				await Task.Delay(TimeSpan.FromSeconds(30));
				if (targetGuild != null && IsConnected)
				{
					try
					{
						var voiceStates = targetGuild.VoiceStates.ToList();
						var members = new Dictionary<ulong, DiscordVoiceState>(voiceStates.Count);
						for (int i = 0; i < voiceStates.Count; i++)
						{
							if (voiceStates[i] == null) continue;
							if (!voiceStates[i].User.IsBot && voiceStates[i].Channel != null && voiceStates[i].Channel.ParentId != 370312545329872896 && voiceStates[i].Channel.ParentId != 370307232329367553 && voiceStates[i].Channel.Id != 384005063183695872)
								members.Add(voiceStates[i].User.Id, voiceStates[i]);
						}
						using (var db = new LiteDatabase($"filename={botConfig.GuildID}_voiceStat.db; journal=false;"))
						{
							var col = db.GetCollection<UserModel>("Users");
							var currentUsers = new List<UserModel>(members.Count);
							for (int i = 0; i < members.Count; i++)
							{
								var member = members.ElementAt(i);
								var dbUser = col.FindOne(x => x.Id == member.Key);
								dbUser.VoiceOnlineTime += member.Value.Channel.ParentId == 370307078247677961 ? (DateTimeOffset.UtcNow - dbUser.LastUpdateTime.Value) * 2 : DateTimeOffset.UtcNow - dbUser.LastUpdateTime.Value;
								dbUser.LastUpdateTime = DateTimeOffset.UtcNow;
								dbUser = await SendTimeGifts(dbUser);
								currentUsers.Add(dbUser);
							}
							if (currentUsers.Any())
								col.Update(currentUsers);
						}
					}
					catch (InvalidOperationException)
					{
						continue;
					}
					catch (Exception ex)
					{
						Log.Error(ex.ToString());
						continue;
					}
				}
			}
		}

		private async void DragonGift()
		{
			nextDragonFly = DateTimeOffset.Now.AddHours(botConfig.DragonCooldown);
			while (true)
			{
				if (!IsConnected) continue;
				try
				{
					if ((nextDragonFly - DateTimeOffset.Now).TotalMinutes < 1)
					{
						nextDragonFly = DateTimeOffset.Now.AddHours(botConfig.DragonCooldown);
						Dictionary<ulong, uint> dragonUsers = default;
						using (var db = new LiteDatabase($"filename={Bot.botConfig.GuildID}_voiceStat.db; journal=false;"))
						{
							var col = db.GetCollection<UserModel>("Users");
							var dbUsers = col.Find(x => x.DragonCount > 0);
							if (dbUsers != null && dbUsers.Any())
							{
								var dbDragonUsers = dbUsers.ToArray();
								dragonUsers = new Dictionary<ulong, uint>(dbDragonUsers.Length);								
								for (int i = 0; i < dbDragonUsers.Length; i++)
								{
									dragonUsers.Add(dbDragonUsers[i].Id, dbDragonUsers[i].DragonCount);
								}
							}
						}
						var embed = new DiscordEmbedBuilder
						{
							Author = new DiscordEmbedBuilder.EmbedAuthor
							{
								Name = "🐲 Бесконечный Дракон"
							},
							Color = DiscordColor.Blurple
						};
						if (dragonUsers.Count == 0)
						{
							embed.Description = "Дракон не нашел достойных его подарка";
							await dragonGiftsChannel.SendMessageAsync(embed: embed);
						}
						else
						{
							var giftSize = 0;
							var chance = random.Next(0, 100);
							switch (chance)
							{
								case var _ when chance < 10:
									giftSize = 0;
									break;
								case var _ when chance < 20:
									giftSize = random.Next(90, 180);
									break;
								case var _ when chance < 50:
									giftSize = random.Next(50, 90);
									break;
								default:
									giftSize = random.Next(20, 50);
									break;
							}
							if (giftSize == 0)
								embed.Description = "Сегодня был сильный туман. Дракон не смог разглядеть поклонителей. Награда на сегодня: 0 🔑";
							else
							{
								foreach (var dragonUser in dragonUsers)
								{
									await NadekoDbExtension.SetCurrencyValue(dragonUser.Key, giftSize * (int)dragonUser.Value);
								}
								embed.Description = $"Дракон наградил {dragonUsers.Count} верных поданных. Каждый получил сегодня: {giftSize} 🔑";
							}
							await dragonGiftsChannel.SendMessageAsync(embed: embed);
						}
					}
					var channelName = $"🐲 Дракон через {(nextDragonFly - DateTimeOffset.Now).Hours}:{(nextDragonFly - DateTimeOffset.Now).Minutes}​​";
					await dragonCategory.ModifyAsync(name: channelName);
					await Task.Delay(TimeSpan.FromMinutes(1));
				}
				catch (Exception ex)
				{
					Log.Error(ex.ToString());
					continue;
				}
			}
		}

		private async void DragonBorn()
		{
			while (true)
			{
				if (!IsConnected) continue;
				try
				{
					using (var db = new LiteDatabase($"filename={botConfig.GuildID}_voiceStat.db; journal=false;"))
					{
						var col = db.GetCollection<UserModel>("Users");
						var dbUsers = col.Find(x => x.DragonEggCount > 0 || x.IncubatorCount > 0).ToImmutableArray();
						var dbUsersToUpdate = new List<UserModel>();
						for (int i = 0; i < dbUsers.Length; i++)
						{
							var dbUser = dbUsers[i];
							if (dbUser.DragonEggCount > 0)
								dbUser = await TryToBornDragon(dbUser, nameof(dbUser.EggRespawnTimes), nameof(dbUser.DragonEggCount));
							if (dbUser.IncubatorCount > 0)
								dbUser = await TryToBornDragon(dbUser, nameof(dbUser.IncubatorRespawnTimes), nameof(dbUser.IncubatorCount));
							dbUsersToUpdate.Add(dbUser);
						}
						if (dbUsersToUpdate.Any())
							col.Update(dbUsersToUpdate);
					}
					await Task.Delay(TimeSpan.FromSeconds(10));
				}
				catch (Exception ex)
				{
					Log.Error(ex.ToString());
					continue;
				}
			}
		}

		private async Task<UserModel> TryToBornDragon(UserModel dbUser, string respawnPropName, string bornerPropName)
		{
			try
			{
				var respawnTime = ReflectionExtension.GetPropValue<List<DateTimeOffset>>(dbUser, respawnPropName);
				var borner = ReflectionExtension.GetPropValue<uint>(dbUser, bornerPropName);
				if (respawnTime == null)
				{
					respawnTime = new List<DateTimeOffset>();
					ReflectionExtension.SetValue(dbUser, respawnPropName, respawnTime);
				}
				uint nonExistTimes = 0;
				nonExistTimes = (uint)Math.Max(0, (int)borner - respawnTime.Count);
				if (nonExistTimes > 0)
				{
					for (int j = 0; j < nonExistTimes; j++)
					{
						if (bornerPropName == nameof(dbUser.IncubatorCount))
							respawnTime.Add(DateTimeOffset.UtcNow.AddHours(24));
						else respawnTime.Add(DateTimeOffset.UtcNow.AddHours(18));
						ReflectionExtension.SetValue(dbUser, respawnPropName, respawnTime);
					}
				}
				var removedCount = respawnTime.RemoveAll(x => (x - DateTimeOffset.UtcNow).TotalSeconds < 1);
				if (removedCount > 0)
				{
					borner = (uint)Math.Max(0, (int)borner - removedCount);
					ReflectionExtension.SetValue(dbUser, bornerPropName, borner);
					var embed = new DiscordEmbedBuilder
					{
						Author = new DiscordEmbedBuilder.EmbedAuthor
						{
							Name = "🐲 Рождение дракона"
						},
						Color = DiscordColor.Blurple
					};
					var originDragonCount = dbUser.DragonCount;
					string prizeText = default;
					for (int j = 0; j < removedCount; j++)
					{
						var chance = Bot.random.Next(0, 100);
						if (bornerPropName == nameof(dbUser.IncubatorCount))
						{
							switch (chance)
							{
								case var _ when chance < 10:
									dbUser.DragonCount += 3;
									prizeText = "3 🐲";
									break;
								case var _ when chance < 35:
									dbUser.DragonCount += 2;
									prizeText = "2 🐲";
									break;
								default:
									dbUser.DragonCount += 1;
									prizeText = "1 🐲";
									break;
							}
						}
						else
						{
							if (chance < 35)
							{
								dbUser.DragonCount += 1;
								prizeText = "1 🐲";
							}
						}
					}
					var member = targetGuild.Members.FirstOrDefault(x => x.Id == dbUser.Id);
					var resultUnicode = bornerPropName == nameof(dbUser.DragonEggCount) ? DiscordEmoji.FromUnicode(client, "🥚") : DiscordEmoji.FromName(client, ":incubator:");
					if (originDragonCount == dbUser.DragonCount)
					{
						embed.Description = member != null ? $"{member.Mention} К сожалению, процесс создания 🐲 из {resultUnicode} завершился неудачей" : $"{dbUser.Id} К сожалению, процесс создания 🐲 из {resultUnicode} завершился неудачей";
						await client.SendMessageAsync(targetGuild.Channels.FirstOrDefault(x => x.Id == 408802925289406475), embed: embed);
					}
					else
					{
						embed.Description = member != null ? $"{member.Mention} Вы получили {prizeText} из {removedCount} {resultUnicode}" : $"{dbUser.Id} Вы получили {prizeText} из {removedCount} {resultUnicode}";
						await client.SendMessageAsync(targetGuild.Channels.FirstOrDefault(x => x.Id == 408802925289406475), embed: embed);
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
				return dbUser;
			}
			return dbUser;
		}
	}
}