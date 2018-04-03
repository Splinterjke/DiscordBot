using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using LiteDB;
using Serilog;
using VoiceOnlineBot;

namespace VoiceOnlineBot
{
	public class Commands
	{
		[Command("giveitem"), Aliases("передать")]
		public async Task GiveItem(CommandContext ctx, uint transactionValue = 1, [Description("имя предмета")]string transactionItem = default, [Description("целевой пользователь")]DiscordMember targetUser = default)
		{
			var embed = new DiscordEmbedBuilder
			{
				Author = new DiscordEmbedBuilder.EmbedAuthor
				{
					Name = "♲ Передача предметов"
				},
				Color = new DiscordColor("#321272")
			};
			try
			{
				await ctx.TriggerTypingAsync();
				if (string.IsNullOrEmpty(transactionItem))
				{
					throw new ArgumentNullException($"Некорретно указан аргумент [{ctx.Command.Arguments[1].Description}]");
				}
				if (targetUser == null)
				{
					throw new ArgumentNullException($"Некорретно указан аргумент [{ctx.Command.Arguments[2].Description}]");
				}

				using (var db = new LiteDatabase($"filename={Bot.botConfig.GuildID}_voiceStat.db; journal=false;"))
				{
					var col = db.GetCollection<UserModel>("Users");
					var sender = col.FindOne(x => x.Id == ctx.Member.Id);
					var dbUser = col.FindOne(x => x.Id == targetUser.Id);
					if (sender == null)
						throw new ArgumentException("Вы не найдены в базе");
					if (dbUser == null)
						throw new ArgumentException($"{targetUser.Username} не найден(а) в базе");
					switch (transactionItem)
					{
						case "яйцо":
							if (sender.DragonEggCount < transactionValue && !Bot.botConfig.OwnerIds.Contains(ctx.User.Id))
								throw new ArgumentException("У Вас недостаточно 🥚");
							dbUser.DragonEggCount += transactionValue;
							sender.DragonEggCount -= transactionValue;
							if (sender.EggRespawnTimes != null)
							{
								for (int j = (int)transactionValue - 1; j >= 0; j--)
								{
									sender.EggRespawnTimes.RemoveAt(j);
								}
							}
							if (dbUser.EggRespawnTimes == null)
								dbUser.EggRespawnTimes = new List<DateTimeOffset>();
							for (int j = 0; j < transactionValue; j++)
							{
								dbUser.EggRespawnTimes.Add(DateTimeOffset.UtcNow.AddHours(18));
							}
							break;
						case "патрон":
							if (sender.AmmoCount < transactionValue && !Bot.botConfig.OwnerIds.Contains(ctx.User.Id))
								throw new ArgumentException($"У Вас недостаточно {DiscordEmoji.FromName(ctx.Client, ":bullet:")}");
							dbUser.AmmoCount += transactionValue;
							sender.AmmoCount -= transactionValue;
							break;
						case "дракон":
							if (sender.DragonCount < transactionValue && !Bot.botConfig.OwnerIds.Contains(ctx.User.Id))
								throw new ArgumentException("У Вас недостаточно 🐲");
							dbUser.DragonCount += transactionValue;
							sender.DragonCount -= transactionValue;
							break;
						case "инкубатор":
							if (sender.IncubatorCount < transactionValue && !Bot.botConfig.OwnerIds.Contains(ctx.User.Id))
								throw new ArgumentException($"У Вас недостаточно {DiscordEmoji.FromName(ctx.Client, ":incubator: ")}");
							dbUser.IncubatorCount += transactionValue;
							sender.IncubatorCount -= transactionValue;
							if (sender.IncubatorRespawnTimes != null)
							{
								for (int j = (int)transactionValue - 1; j >= 0; j--)
								{
									sender.IncubatorRespawnTimes.RemoveAt(j);
								}
							}
							if (dbUser.IncubatorRespawnTimes == null)
								dbUser.IncubatorRespawnTimes = new List<DateTimeOffset>();
							for (int j = 0; j < transactionValue; j++)
							{
								dbUser.IncubatorRespawnTimes.Add(DateTimeOffset.UtcNow.AddHours(24));
							}
							break;
						case "краска":
							if (sender.PaintCount < transactionValue && !Bot.botConfig.OwnerIds.Contains(ctx.User.Id))
								throw new ArgumentException("У Вас недостаточно 🎨");
							dbUser.PaintCount += transactionValue;
							sender.PaintCount -= transactionValue;
							break;
						case "пистолет":
							if (sender.PistolCount < transactionValue && !Bot.botConfig.OwnerIds.Contains(ctx.User.Id))
								throw new ArgumentException("У Вас недостаточно 🔫");
							dbUser.PistolCount += transactionValue;
							sender.PistolCount -= transactionValue;
							break;
						case "пак":
							if (sender.GiftCount < transactionValue && !Bot.botConfig.OwnerIds.Contains(ctx.User.Id))
								throw new ArgumentException("У Вас недостаточно 🎁");
							dbUser.GiftCount += transactionValue;
							sender.GiftCount -= transactionValue;
							break;
						default:
							throw new ArgumentException("Данный предмет невозможно передать");
					}
					col.Update(dbUser);
					if (!Bot.botConfig.OwnerIds.Contains(ctx.User.Id))
					{
						col.Update(sender);
					}
					embed.Description = $"{targetUser.Mention} Вы получили {transactionValue} {transactionItem} от {ctx.User.Mention}";
					await ctx.RespondAsync(embed: embed);
				}
			}
			catch (ArgumentException ex)
			{
				embed.Description = $"{ctx.User.Mention} {ex.Message}";
				await ctx.RespondAsync(embed: embed);
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
			}
		}

		[Command("награда"), Aliases("timely")]
		public async Task Timely(CommandContext ctx)
		{
			var embed = new DiscordEmbedBuilder
			{
				Author = new DiscordEmbedBuilder.EmbedAuthor
				{
					Name = "🎁 Ежедневная награда"
				},
				Color = new DiscordColor("#321272")
			};
			try
			{
				await ctx.TriggerTypingAsync();
				using (var db = new LiteDatabase($"filename={Bot.botConfig.GuildID}_voiceStat.db; journal=false;"))
				{
					var dbUsers = db.GetCollection<UserModel>("Users");
					var dbUser = dbUsers.FindOne(x => x.Id == ctx.User.Id);
					if (dbUser == null)
						throw new ArgumentException("Вы не найдены в базе");

					if (dbUser.VoiceOnlineTime.TotalHours > 2)
					{
						if (dbUser.LastPackUsedTime == null || (DateTimeOffset.UtcNow - dbUser.LastPackUsedTime).Value.TotalSeconds > 24 * 60 * 60)
						{
							dbUser.GiftCount += 1;
							dbUser.LastPackUsedTime = DateTimeOffset.UtcNow;
							dbUsers.Update(dbUser);
							embed.Description = $"{ctx.User.Mention} Вы забрали 1 ежедневный 🎁. Чтобы узнать кол.-во 🎁 используйте команду !me";
						}
						else
						{
							var nextPackTime = dbUser.LastPackUsedTime.Value.AddSeconds(24 * 60 * 60) - DateTimeOffset.UtcNow;
							embed.Description = $"{ctx.User.Mention} Вы сможете забрать следующую награду через {nextPackTime.Hours} ч. {nextPackTime.Minutes} мин.";
						}
					}
					else embed.Description = $"{ctx.User.Mention} Необходимо иметь хотя бы 2 часа голосового онлайна.\nЧтобы узнать время своего голос. онлайна, используйте команду !мойонлайн";
				}
				await ctx.RespondAsync(embed: embed);
			}
			catch (ArgumentException ex)
			{
				embed.Description = $"{ctx.Member.Mention} {ex.Message}";
				await ctx.RespondAsync(embed: embed);
			}
			catch (Exception ex)
			{
				Log.Error($"Exception occured: {ex.GetType()}: {ex.Message} {ex.StackTrace}");
			}
			return;
		}

		[Command("me"), Aliases("я")]
		public async Task Inventory(CommandContext ctx, DiscordMember targetUser = default)
		{
			var embed = new DiscordEmbedBuilder
			{
				Author = new DiscordEmbedBuilder.EmbedAuthor
				{
					Name = "👤 Профиль"
				},
				Color = new DiscordColor("#321272")
			};
			try
			{
				await ctx.TriggerTypingAsync();
				if (targetUser == null)
					targetUser = ctx.Member;
				using (var db = new LiteDatabase($"filename={Bot.botConfig.GuildID}_voiceStat.db; journal=false;"))
				{
					var dbUsers = db.GetCollection<UserModel>("Users");
					var dbUser = dbUsers.FindOne(x => x.Id == targetUser.Id);
					if (dbUser == null)
					{
						if (targetUser.Id == ctx.Member.Id)
							throw new ArgumentException("Вы не найдены в базе");
						else throw new ArgumentException($"{targetUser.Username} не найден(а) в базе");
					}

					string closestEggRespawn = dbUser.EggRespawnTimes != null && dbUser.EggRespawnTimes.Count > 0 ? $" (ост. {(dbUser.EggRespawnTimes[0] - DateTimeOffset.UtcNow).Hours}ч. {(dbUser.EggRespawnTimes[0] - DateTimeOffset.UtcNow).Minutes} мин. {(dbUser.EggRespawnTimes[0] - DateTimeOffset.UtcNow).Seconds} сек.)" : default;
					string closestIncubatorRespawn = dbUser.IncubatorRespawnTimes != null && dbUser.IncubatorRespawnTimes.Count > 0 ? $" (ост. {(dbUser.IncubatorRespawnTimes[0] - DateTimeOffset.UtcNow).Hours}ч. {(dbUser.IncubatorRespawnTimes[0] - DateTimeOffset.UtcNow).Minutes} мин. {(dbUser.IncubatorRespawnTimes[0] - DateTimeOffset.UtcNow).Seconds} сек.)" : default;
					var smallShieldText = "нет";
					if (dbUser.SmallShieldEndTime != null)
					{
						var smallShieldTime = dbUser.SmallShieldEndTime.Value - DateTimeOffset.UtcNow;
						if (smallShieldTime.TotalSeconds <= 0)
						{
							dbUser.SmallShieldEndTime = null;
							dbUsers.Update(dbUser);
						}
						else smallShieldText = $"ост. {smallShieldTime.Days} д. {smallShieldTime.Hours} ч. {smallShieldTime.Minutes} мин.";
					}
					var supperShieldText = dbUser.SuperShieldEndurance > 0 ? $"прочность {dbUser.SuperShieldEndurance}" : "нет";
					var marriageText = "одинок(а)";
					if (dbUser.PartnerId != 0)
					{
						var partner = ctx.Guild.Members.FirstOrDefault(x => x.Id == dbUser.PartnerId);
						var partnerName = partner != null ? partner.Username : dbUser.PartnerId.ToString();
						if (dbUser.MarriageTime != null)
						{
							if ((dbUser.MarriageTime.Value - DateTimeOffset.UtcNow).TotalSeconds < 1)
							{
								dbUser.MarriageTime = default;
								var partnerDb = dbUsers.FindOne(x => x.Id == dbUser.PartnerId);
								partnerDb.MarriageTime = default;
								marriageText = $"обручен(а) c {partnerName}";
								dbUsers.Update(dbUser);
								dbUsers.Update(partnerDb);
							}
							else marriageText = $"бракосочетание через {(dbUser.MarriageTime.Value - DateTimeOffset.UtcNow).Days} д. {(dbUser.MarriageTime.Value - DateTimeOffset.UtcNow).Hours} ч. {(dbUser.MarriageTime.Value - DateTimeOffset.UtcNow).Minutes} мин. {(dbUser.MarriageTime.Value - DateTimeOffset.UtcNow).Seconds} сек.";
						}
						else
							marriageText = $"обручен(а) c {partnerName}";
					}
					var keys = await NadekoDbExtension.GetCurrencyValue(targetUser.Id);
					embed.Description = $"{targetUser.Mention}\n"
						+ $"Семейный статус: {marriageText}\n"
						+ "Боевой статус: новичок";
					embed.AddField("🛡 Малый щит", smallShieldText, true);
					embed.AddField("🛡 Боевой щит", supperShieldText, true);
					embed.AddField("🎁 Паки", dbUser.GiftCount.ToString(), true);
					embed.AddField("🥚 Яйца", dbUser.DragonEggCount + closestEggRespawn, true);
					embed.AddField($"{DiscordEmoji.FromName(ctx.Client, ":incubator:")} Инкубаторы", dbUser.IncubatorCount + closestIncubatorRespawn, true);
					embed.AddField("🐲 Драконы", dbUser.DragonCount.ToString(), true);
					embed.AddField("🔫 Пистолеты", dbUser.PistolCount.ToString(), true);
					embed.AddField("🎨 Краски", dbUser.PaintCount.ToString(), true);
					embed.AddField($"{DiscordEmoji.FromName(ctx.Client, ":bullet:")} Патроны", dbUser.AmmoCount.ToString(), true);
					embed.AddField("Удачных походов", "Данж #1: 0\n"
						+ "Данж #2: 0\n"
						+ "Данж #3: 0\n"
						+ "Данж #4: 0\n"
						+ "Данж #5: 0\n", true);
					embed.AddField("Всего награблено", "0", true);
					embed.AddField("🔑 Ключи", keys.ToString(), true);
					embed.AddField("💍 Кольца", dbUser.RingCount.ToString(), true);
					embed.AddField("Инфо", "Чтобы распаковать 🎁, используйте команду !распаковать", false);
				}
				await ctx.RespondAsync(embed: embed);
			}
			catch (ArgumentException ex)
			{
				embed.Description = $"{ctx.Member.Mention} {ex.Message}";
				await ctx.RespondAsync(embed: embed);
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
			}
			return;
		}

		// [Command("test")]
		// public async Task Test(CommandContext ctx, uint value = default, string item = default, DiscordMember targtUser = null)
		// {
		// 	await ctx.TriggerTypingAsync();
		// 	var embed = new DiscordEmbedBuilder
		// 	{
		// 		Author = new DiscordEmbedBuilder.EmbedAuthor
		// 		{
		// 			Name = "Тестовая команда"
		// 		},
		// 		Description = $"{ctx.User.Mention}, У вас {NadekoDbExtension.GetCurrencyValue(ctx.User.Id)} 🔑",
		// 		Color = new DiscordColor("#321272"),
		// 		Footer = new DiscordEmbedBuilder.EmbedFooter
		// 		{
		// 			Text = "http://194.67.207.194",
		// 		}
		// 	};
		// 	await ctx.RespondAsync(embed: embed);
		// }

		[Command("распаковать"), Aliases("unpack")]
		public async Task Unpack(CommandContext ctx, uint unpackCount = 1)
		{
			var embed = new DiscordEmbedBuilder
			{
				Author = new DiscordEmbedBuilder.EmbedAuthor
				{
					Name = "🎁 распаковка"
				},
				Color = new DiscordColor("#321272")
			};
			try
			{
				await ctx.TriggerTypingAsync();
				if (unpackCount <= 0)
					throw new ArgumentException("Некорректно указан аргумент");
				using (var db = new LiteDatabase($"filename={Bot.botConfig.GuildID}_voiceStat.db; journal=false;"))
				{
					var dbUsers = db.GetCollection<UserModel>("Users");
					var dbUser = dbUsers.FindOne(x => x.Id == ctx.User.Id);
					if (dbUser == null)
						throw new ArgumentException("Вы не найдены в базе");
					if (dbUser.GiftCount == 0)
						throw new ArgumentException("У вас нет 🎁.\nЧтобы собрать ежедневную награду, используйте команду !награда");
					if (unpackCount > dbUser.GiftCount)
						throw new ArgumentException("У вас нет указанного кол.-тва 🎁.\nЧтобы собрать ежедневную награду, используйте команду !награда");
					if (unpackCount == 1)
					{
						dbUser.GiftCount--;
					}
					else if (unpackCount == 10 || unpackCount == 30)
					{
						dbUser.GiftCount -= unpackCount;
					}
					else
						throw new ArgumentException("Распаковать можно только 1, 10 или 30 🎁.\nЧтобы распаковать 1 🎁, используйте команду !распаковать.\nИспользуйте !распаковать 10 или !распаковать 30, чтобы получить супер-награду.");
					var prizeText = "";
					var prizeValue = 0;
					if (unpackCount == 10)
					{
						var chance = Bot.random.Next(0, 100);
						switch (chance)
						{
							case var _ when chance < 10:
								dbUser.AmmoCount += 10;
								prizeText = $"10 {DiscordEmoji.FromName(ctx.Client, ":bullet:")}";
								break;
							case var _ when chance < 20:
								prizeValue = Bot.random.Next(1000, 2000);
								break;
							case var _ when chance < 40:
								prizeText = $"1 🎨";
								dbUser.PaintCount++;
								break;
							default:
								prizeValue = Bot.random.Next(300, 1000);
								break;
						}
					}
					else
					if (unpackCount == 30)
					{
						var chance = Bot.random.Next(0, 100);
						switch (chance)
						{
							case var _ when chance < 10:
								dbUser.PistolCount++;
								prizeText = $"1 🔫";
								break;
							case var _ when chance < 10:
								dbUser.DragonEggCount++;
								if (dbUser.EggRespawnTimes == null)
									dbUser.EggRespawnTimes = new List<DateTimeOffset>();
								dbUser.EggRespawnTimes.Add(DateTimeOffset.UtcNow.AddHours(18));
								prizeText = $"1 🥚";
								break;
							case var _ when chance < 40:
								if (ctx.Member.Roles.Contains(Bot.prizeRole))
									throw new ArgumentException($"У вас уже есть роль {Bot.prizeRole.Name}, 🎁 не списываются");
								await ctx.Guild.GrantRoleAsync(ctx.Member, Bot.prizeRole);
								prizeText = $"роль {Bot.botConfig.PrizeRoleName}";
								break;
							default:
								prizeValue = Bot.random.Next(1500, 3500);
								break;
						}
					}
					else
					{
						var chance = Bot.random.Next(0, 100);
						switch (chance)
						{
							case var _ when chance < 10:
								prizeValue = Bot.random.Next(120, 180);
								break;
							case var _ when chance < 40:
								prizeValue = Bot.random.Next(80, 120);
								break;
							default:
								prizeValue = Bot.random.Next(10, 80);
								break;
						}
					}
					if (prizeValue != 0)
					{
						prizeText = $"{prizeValue} 🔑";
						await NadekoDbExtension.SetCurrencyValue(ctx.User.Id, prizeValue);
					}
					dbUsers.Update(dbUser);
					embed.Description = $"{ctx.User.Mention} Вы распаковали 🎁 и получили {prizeText}";
					await ctx.RespondAsync(embed: embed);
				}
			}
			catch (ArgumentException ex)
			{
				embed.Description = $"{ctx.Member.Mention} {ex.Message}";
				await ctx.RespondAsync(embed: embed);
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
			}
			return;
		}

		[Command("rolecolor"), Aliases("цветроли")]
		public async Task RoleColorAsync(CommandContext ctx, string colorCode = default, [Description("целевая роль")]DiscordRole targetRole = default)
		{
			var embed = new DiscordEmbedBuilder
			{
				Author = new DiscordEmbedBuilder.EmbedAuthor
				{
					Name = "🎨 Цвет роли"
				},
				Color = new DiscordColor("#321272")
			};
			try
			{
				await ctx.TriggerTypingAsync();
				if (targetRole == null)
					throw new ArgumentException($"Некорректно указана роль [{ctx.Command.Arguments[1].Description}]");
				if (!ctx.Member.Roles.Contains(targetRole))
					throw new ArgumentException("Изменять можно только свои роли");
				if (!colorCode.StartsWith('#') || colorCode.Length > 7)
					throw new ArgumentException("Некорректно указан код цвета. Формат цвета шестнадцатиричный #FFFFFF");

				using (var db = new LiteDatabase($"filename={Bot.botConfig.GuildID}_voiceStat.db; journal=false;"))
				{
					var dbRoles = db.GetCollection<RoleModel>("Roles");
					dbRoles.EnsureIndex(x => x.Id);
					var dbRole = dbRoles.FindOne(x => x.Id == targetRole.Id);
					if (dbRole == null)
					{
						var roleModel = new RoleModel
						{
							Id = targetRole.Id,
							LastPaintUsedTime = null
						};
						dbRoles.Insert(roleModel);
						dbRole = dbRoles.FindOne(x => x.Id == targetRole.Id);
					}
					if (dbRole.LastPaintUsedTime != null && (dbRole.LastPaintUsedTime.Value.AddDays(14) - DateTimeOffset.UtcNow).TotalMinutes > 0)
						throw new ArgumentException($"Цвет роли можно будет изменить через {(dbRole.LastPaintUsedTime.Value.AddDays(14) - DateTimeOffset.UtcNow).Days} д. {(dbRole.LastPaintUsedTime.Value.AddDays(14) - DateTimeOffset.UtcNow).Hours} ч. {(dbRole.LastPaintUsedTime.Value.AddDays(14) - DateTimeOffset.UtcNow).Minutes} мин.\n"
							+ $"Проверить кол.-во 🎨 можно командой !me");
					var dbUsers = db.GetCollection<UserModel>("Users");
					var dbUser = dbUsers.FindOne(x => x.Id == ctx.User.Id);
					if (dbUser == null)
						throw new ArgumentException("Вы не найдены в базе");
					if (dbUser.PaintCount == 0)
						throw new ArgumentException("У вас недостаточно 🎨");
					await ctx.Guild.UpdateRoleAsync(targetRole, color: new DiscordColor(colorCode));
					dbUser.PaintCount--;
					dbRole.LastPaintUsedTime = DateTimeOffset.UtcNow;
					dbRoles.Update(dbRole);
					dbUsers.Update(dbUser);
					embed.Description = $"{ctx.User.Mention} Цвет роли {targetRole.Name} успешно изменен";
					await ctx.RespondAsync(embed: embed);
				}
			}
			catch (ArgumentException ex)
			{
				embed.Description = $"{ctx.Member.Mention} {ex.Message}";
				await ctx.RespondAsync(embed: embed);
			}
			catch (Exception ex)
			{
				Log.Error($"Exception occured: {ex.GetType()}: {ex.Message} {ex.StackTrace}");
			}
			return;
		}

		[Command("rolename"), Aliases("имяроли")]
		public async Task RoleNameAsync(CommandContext ctx, [Description("целевая роль")]DiscordRole targetRole = default, [Description("новое имя роли")]string roleName = default)
		{
			var embed = new DiscordEmbedBuilder
			{
				Author = new DiscordEmbedBuilder.EmbedAuthor
				{
					Name = "🎁 Изменение роли"
				},
				Color = new DiscordColor("#321272")
			};
			try
			{
				await ctx.TriggerTypingAsync();
				if (targetRole == null)
					throw new ArgumentException($"Некорректно указана аргумент [{ctx.Command.Arguments[0].Description}]");
				if (string.IsNullOrEmpty(roleName))
					throw new ArgumentException($"Некорректно указан аргумент [{ctx.Command.Arguments[1].Description}]\nИспользуйте !роль @роль [название_роли], чтобы изменить название Вашей роли");
				if (!ctx.Member.Roles.Contains(targetRole))
					throw new ArgumentException("Изменять можно только свои роли");
				if (await NadekoDbExtension.GetCurrencyValue(ctx.User.Id) < 1800)
					throw new ArgumentException("У вас недостаточно 🔑. Необходимо 1800 🔑");

				var oldRoleName = targetRole.Name;
				await ctx.Guild.UpdateRoleAsync(targetRole, name: roleName + "⠀");
				await NadekoDbExtension.SetCurrencyValue(ctx.User.Id, -1800);
				embed.Description = $"{ctx.User.Mention} Название роли {oldRoleName} успешно изменено на {roleName}";
				await ctx.RespondAsync(embed: embed);
			}
			catch (ArgumentException ex)
			{
				embed.Description = $"{ctx.Member.Mention} {ex.Message}";
				await ctx.RespondAsync(embed: embed);
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
			};
		}

		[Command("roletransfer"), Aliases("передатьроль")]
		public async Task RoleTransferAsync(CommandContext ctx, [Description("целевая роль")]DiscordRole targetRole = default, [Description("целевой пользователь")]DiscordMember targerUser = default)
		{
			var embed = new DiscordEmbedBuilder
			{
				Author = new DiscordEmbedBuilder.EmbedAuthor
				{
					Name = "🎁 Передача роли"
				},
				Color = new DiscordColor("#321272")
			};
			try
			{
				await ctx.TriggerTypingAsync();
				if (targetRole == null)
					throw new ArgumentException($"Некорректно указана аргумент [{ctx.Command.Arguments[0].Description}]");
				if (targerUser == null)
					throw new ArgumentException($"Некорректно указан аргумент [{ctx.Command.Arguments[1].Description}]\nИспользуйте !роль @роль @пользователь, чтобы передать Вашу роль");
				if (!ctx.Member.Roles.Contains(targetRole))
					throw new ArgumentException("Изменять можно только свои роли");
				if (await NadekoDbExtension.GetCurrencyValue(ctx.User.Id) < 1800)
					throw new ArgumentException("У вас недостаточно 🔑");

				await targerUser.GrantRoleAsync(targetRole);
				await ctx.Member.RevokeRoleAsync(targetRole);
				await NadekoDbExtension.SetCurrencyValue(ctx.User.Id, -1800);
				embed.Description = $"{ctx.User.Mention} Роль {targetRole.Name} успешно передана {targerUser.Username}";
				await ctx.RespondAsync(embed: embed);
			}
			catch (ArgumentException ex)
			{
				embed.Description = $"{ctx.Member.Mention} {ex.Message}";
				await ctx.RespondAsync(embed: embed);
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
			};
		}

		[Command("sellrole"), Aliases("продатьроль")]
		public async Task SellRole(CommandContext ctx, DiscordRole targetRole = default)
		{
			var embed = new DiscordEmbedBuilder
			{
				Author = new DiscordEmbedBuilder.EmbedAuthor
				{
					Name = "🎁 Продажа роли"
				},
				Color = new DiscordColor("#321272")
			};
			try
			{
				await ctx.TriggerTypingAsync();
				if (targetRole == null)
					throw new ArgumentException("Некорректно указана роль");
				if (!ctx.Member.Roles.ToHashSet().Contains(targetRole))
					throw new ArgumentException("Вы не являетесь участником этой роли");
				if (ctx.Guild.Members.Where(x => !x.IsBot && x.Roles.ToHashSet().Contains(targetRole)).ToArray().Length > 1)
					throw new ArgumentException("Роль можно продать, если в ней только 1 участник");
				await ctx.Member.RevokeRoleAsync(targetRole);
				await NadekoDbExtension.SetCurrencyValue(ctx.User.Id, 4000);
				embed.Description = $"{ctx.User.Mention} Роль {targetRole.Name} успешно продана за 4000 🔑";
				await ctx.RespondAsync(embed: embed);
			}
			catch (ArgumentException ex)
			{
				embed.Description = $"{ctx.Member.Mention} {ex.Message}";
				await ctx.RespondAsync(embed: embed);
			}
			catch (Exception ex)
			{
				Log.Error($"Exception occured: {ex.GetType()}: {ex.Message} {ex.StackTrace}");
			}
		}

		[Command("reset"), Hidden]
		public Task Reset(CommandContext ctx)
		{
			if (!ctx.Channel.IsPrivate) return Task.CompletedTask;
			try
			{
				var isParsed = ulong.TryParse(ctx.RawArgumentString, out ulong userId);
				if (!isParsed) return Task.CompletedTask;
				using (var db = new LiteDatabase($"filename={Bot.botConfig.GuildID}_voiceStat.db; journal=false;"))
				{
					var col = db.GetCollection<UserModel>("Users");
					var dbUser = col.FindOne(x => x.Id == userId);
					if (dbUser == null) return Task.CompletedTask;
					dbUser.IsConnected = false;
					dbUser.VoiceOnlineTime = TimeSpan.Zero;
					dbUser.LastSessionTime = null;
					dbUser.LastConnectedTime = null;
					col.Update(dbUser);
				}
			}
			catch (Exception ex)
			{
				Log.Error($"Exception occured: {ex.GetType()}: {ex.Message} {ex.StackTrace}");
			}
			return Task.CompletedTask;
		}

		[Command("dragontrigger"), Hidden]
		public Task DragonTrigger(CommandContext ctx)
		{
			if (ctx.User.Id != 149834096401448960) return Task.CompletedTask;
			Bot.nextDragonFly = DateTimeOffset.Now;
			return Task.CompletedTask;
		}

		[Command("dragontransfer"), Hidden]
		public Task DragonTransfer(CommandContext ctx, DiscordMember targetUser, uint value)
		{
			try
			{
				if (ctx.User.Id != 149834096401448960) return Task.CompletedTask;
				if (targetUser == null)
					throw new Exception("некорректно указан пользователь");
				using (var db = new LiteDatabase($"filename={Bot.botConfig.GuildID}_voiceStat.db; journal=false;"))
				{
					var col = db.GetCollection<UserModel>("Users");
					var dbUser = col.FindOne(x => x.Id == targetUser.Id);
					if (dbUser == null)
						throw new Exception($"{targetUser.Username} не найден(а) в базе");
					dbUser.DragonCount += value;
					col.Update(dbUser);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
			}
			return Task.CompletedTask;
		}

		[Command("топ"), Aliases("top"), Cooldown(1, reset: 30, bucket_type: CooldownBucketType.Guild)]
		public async Task TopAsync(CommandContext ctx)
		{
			await ctx.TriggerTypingAsync();
			var embed = new DiscordEmbedBuilder
			{
				Author = new DiscordEmbedBuilder.EmbedAuthor
				{
					Name = "Лидеры голосового онлайна",
					Url = "http://194.67.207.194"
				},
				Color = new DiscordColor("#321272"),
				ThumbnailUrl = "https://media.discordapp.net/attachments/280945624084381698/399614111266373632/rank.png",

				Footer = new DiscordEmbedBuilder.EmbedFooter
				{
					Text = "Полная статистика на http://194.67.207.194",
				}
			};

			List<UserModel> positiveOnlineUsers;
			using (var db = new LiteDatabase($"filename={Bot.botConfig.GuildID}_voiceStat.db; journal=false;"))
			{
				var col = db.GetCollection<UserModel>("Users");
				positiveOnlineUsers = col.Find(x => x.VoiceOnlineTime != TimeSpan.Zero).OrderByDescending(x => x.VoiceOnlineTime).Take(10).ToList();
			}
			for (int i = 0; i < positiveOnlineUsers.Count; i++)
			{
				var place = $"{i + 1}ое место";
				var medal = ":medal:";
				if (i == 0)
					medal = ":first_place:";
				else if (i == 1)
					medal = ":second_place:";
				else if (i == 2)
					medal = ":third_place:";
				if (i == 3)
					place = $"{i + 1}е место";
				if (positiveOnlineUsers[i].Username.Length > 36)
					embed.AddField($"{medal} {place}", positiveOnlineUsers[i].Username.Substring(0, 36), true);
				else embed.AddField($"{medal} {place}", positiveOnlineUsers[i].Username, true);
				embed.AddField(":clock4:", $"{positiveOnlineUsers[i].VoiceOnlineTime.Hours + positiveOnlineUsers[i].VoiceOnlineTime.Days * 24} ч. {positiveOnlineUsers[i].VoiceOnlineTime.Minutes} мин. {positiveOnlineUsers[i].VoiceOnlineTime.Seconds} сек.", true);
			}
			await ctx.Client.SendMessageAsync(ctx.Channel, embed: embed);
		}

		[Command("мойонлайн"), Aliases("myonline")]
		public async Task MyOnlineAsync(CommandContext ctx)
		{
			await ctx.TriggerTypingAsync();
			UserModel dbUser = null;
			int dbUserIndex = -1;
			using (var db = new LiteDatabase($"filename={Bot.botConfig.GuildID}_voiceStat.db; journal=false;"))
			{
				var col = db.GetCollection<UserModel>("Users");
				var tempModel = col.FindAll().OrderByDescending(x => x.VoiceOnlineTime).Select((user, index) => new { user, index }).FirstOrDefault(x => x.user.Id == ctx.User.Id);
				if (tempModel != null)
				{
					dbUser = tempModel.user;
					dbUserIndex = tempModel.index;
				}
			}
			var embed = new DiscordEmbedBuilder
			{
				Author = new DiscordEmbedBuilder.EmbedAuthor
				{
					Name = $"Статистика голосового онлайна",
					Url = "http://194.67.207.194"
				},
				Color = new DiscordColor("#321272"),
				ThumbnailUrl = ctx.User.AvatarUrl,

				Footer = new DiscordEmbedBuilder.EmbedFooter
				{
					Text = "Полная статистика на http://194.67.207.194",
				}
			};
			if (dbUser != null && dbUserIndex != -1)
			{
				var place = $"{dbUserIndex + 1} место";
				var medal = ":medal:";
				if (dbUserIndex == 0)
					medal = ":first_place:";
				else if (dbUserIndex == 1)
					medal = ":second_place:";
				else if (dbUserIndex == 2)
					medal = ":third_place:";
				if (dbUserIndex == 3)
					place = $"{dbUserIndex + 1}е место";
				if (dbUser.Username.Length > 36)
					embed.AddField($"{medal} {place}", dbUser.Username.Substring(0, 36), true);
				else embed.AddField($"{medal} {place}", dbUser.Username, true);
				embed.AddField(":clock4:", $"{dbUser.VoiceOnlineTime.Hours + dbUser.VoiceOnlineTime.Days * 24} ч. {dbUser.VoiceOnlineTime.Minutes} мин. {dbUser.VoiceOnlineTime.Seconds} сек.", true);
			}
			else
			{
				embed.Description = "Вы не найдены в базе.";
			}
			await ctx.Client.SendMessageAsync(ctx.Channel, embed: embed);
		}

		[Command("цитировать"), Aliases("quote")]
		public async Task QuoteAsync(CommandContext ctx)
		{
			if (ctx.Channel.IsPrivate)
				return;
			var arg = ctx.RawArgumentString?.Trim();
			if (string.IsNullOrEmpty(arg) || !ulong.TryParse(arg, out ulong id))
			{
				await ctx.RespondAsync("```Используйте !quote MessageID для цитирования сообщений.```");
				return;
			}
			await ctx.Message.DeleteAsync();
			await ctx.TriggerTypingAsync();
			DiscordMessage quoteMessage = null;
			quoteMessage = await FindMessage(ctx, id);
			if (quoteMessage == null)
			{
				await ctx.RespondAsync("```Не удалось найти сообщение с указанным ID.```");
				return;
			}

			var embed = new DiscordEmbedBuilder
			{
				Author = new DiscordEmbedBuilder.EmbedAuthor
				{
					Name = quoteMessage.Author.Username,
					IconUrl = quoteMessage.Author.AvatarUrl
				},
				Description = quoteMessage.Content,
				Color = new DiscordColor("#329EFB")
			};
			await ctx.RespondAsync(embed: embed.Build());
		}

		[Command("givelikecheck")]
		public async Task GiveLikeCheck(CommandContext ctx)
		{
			if (ctx.Channel.IsPrivate || !Bot.botConfig.LikeCheckOwnersIds.Contains(ctx.User.Id))
				return;
			var embed = new DiscordEmbedBuilder
			{
				Author = new DiscordEmbedBuilder.EmbedAuthor
				{
					Name = $"Выдача лайка, галки",
				},
				Color = new DiscordColor("#321272"),
			};

			var recipients = ctx.Message.MentionedUsers;
			if (recipients != null && recipients.Count > 1)
			{
				embed.Description = $"{ctx.User.Mention} Нельзя упоминать нескольких пользователей";
				await ctx.RespondAsync(embed: embed);
				return;
			}
			var targetUser = recipients[0] as DiscordMember;
			DiscordRole targetRole = null;
			if (ctx.RawArgumentString != null && (ctx.RawArgumentString.Contains("лайк") || ctx.RawArgumentString.Contains("like")))
				targetRole = ctx.Guild.Roles.FirstOrDefault(x => x.Name == "👍");
			else if (ctx.RawArgumentString != null && (ctx.RawArgumentString.Contains("галочка") || ctx.RawArgumentString.Contains("галка")))
				targetRole = ctx.Guild.Roles.FirstOrDefault(x => x.Name == "✔");
			else if (ctx.RawArgumentString != null && (ctx.RawArgumentString.Contains("жен")))
				targetRole = ctx.Guild.Roles.FirstOrDefault(x => x.Name == "♀");
			else if (ctx.RawArgumentString != null && (ctx.RawArgumentString.Contains("муж")))
				targetRole = ctx.Guild.Roles.FirstOrDefault(x => x.Name == "♂");
			if (targetRole == null)
			{
				embed.Description = $"{ctx.User.Mention} Неверно указана роль.\nНужно писать 'лайк' или 'like', или 'галка', или 'галочка'";
				await ctx.RespondAsync(embed: embed);
				return;
			}
			if (targetUser.Roles.Contains(targetRole))
			{
				// if (targetRole.Name == "✔")
				// {
				// 	embed.Description = $"{ctx.User.Mention} Роль уже есть у {targetUser.Mention}";
				// 	await ctx.RespondAsync(embed: embed);
				// 	return;
				// }
				embed.Description = $"{ctx.User.Mention} Роль {targetRole.Name} убрана у {targetUser.Mention}";
				await (targetUser as DiscordMember).RevokeRoleAsync(targetRole);
				await ctx.RespondAsync(embed: embed);
			}
			else
			{
				embed.Description = $"{ctx.User.Mention} Роль выдана {targetUser.Mention}";
				await (targetUser as DiscordMember).GrantRoleAsync(targetRole);
				await ctx.RespondAsync(embed: embed);
			}
		}

		[Command("marriage"), Aliases("свадьба")]
		public async Task MarriageAsync(CommandContext ctx, [Description("тип свадьбы")]string marriageType = default, [Description("целевой пользователь")]DiscordMember targetUser = default)
		{
			var embed = new DiscordEmbedBuilder
			{
				Author = new DiscordEmbedBuilder.EmbedAuthor
				{
					Name = $"💒 Свадьба",
				},
				Color = new DiscordColor("#321272"),
			};
			try
			{
				if (string.IsNullOrEmpty(marriageType))
					throw new ArgumentException($"Некорректно указан аргумент [{ctx.Command.Arguments[0].Description}].\nИспользуйте !свадьба [хрустальная/золотая/бриллиантовая] @user");

				if (targetUser == null)
					throw new ArgumentException($"Некорректно указан аргумент [{ctx.Command.Arguments[1].Description}].\nИспользуйте !свадьба [хрустальная/золотая/бриллиантовая] @user");
				using (var db = new LiteDatabase($"filename={Bot.botConfig.GuildID}_voiceStat.db; journal=false;"))
				{
					var col = db.GetCollection<UserModel>("Users");
					var dbUser = col.FindOne(x => x.Id == targetUser.Id);
					var sender = col.FindOne(x => x.Id == ctx.Member.Id);
					if (dbUser == null)
						throw new ArgumentException($"{targetUser.Username} не найден(а) в базе");
					if (sender == null)
						throw new ArgumentException("Вы не найдены в базе");
					if (sender.MarriageTime != null)
					{
						if ((sender.MarriageTime.Value - DateTimeOffset.Now).TotalSeconds > 0)
							throw new ArgumentException($"У Вас уже назначена свадьба через {(sender.MarriageTime.Value - DateTimeOffset.Now).Hours} ч. {(sender.MarriageTime.Value - DateTimeOffset.Now).Minutes} мин. {(sender.MarriageTime.Value - DateTimeOffset.Now).Seconds} сек.");
						else
						{
							sender.MarriageTime = default;
							col.Update(sender);
							var partner = col.FindOne(x => x.Id == sender.PartnerId);
							if (partner != null)
							{
								partner.MarriageTime = default;
								col.Update(partner);
							}
						}
					}
					if (dbUser.MarriageTime != null)
					{
						if ((dbUser.MarriageTime.Value - DateTimeOffset.Now).TotalSeconds > 0)
							throw new ArgumentException($"У {targetUser.Username} уже назначена свадьба через {(dbUser.MarriageTime.Value - DateTimeOffset.Now).Hours} ч. {(dbUser.MarriageTime.Value - DateTimeOffset.Now).Minutes} мин. {(sender.MarriageTime.Value - DateTimeOffset.Now).Seconds} сек.");
						else
						{
							dbUser.MarriageTime = default;
							col.Update(dbUser);
							var partner = col.FindOne(x => x.Id == dbUser.PartnerId);
							if (partner != null)
							{
								partner.MarriageTime = default;
								col.Update(partner);
							}
						}
					}
					if (sender.PartnerId != 0)
						throw new ArgumentException("Вы уже обручены. Чтобы развестись, используйте команду !развестись");
					if (dbUser.PartnerId != 0)
						throw new ArgumentException($"{targetUser.Username} уже обручен(а).");
					var senderCurrency = await NadekoDbExtension.GetCurrencyValue(ctx.Member.Id);
					switch (marriageType)
					{
						case "хрустальная":
							if (sender.RingCount < 2 || sender.GiftCount < 60 || senderCurrency < 15000)
								throw new ArgumentException("Для того чтобы сыграть Хрустальную свадьбу Вам необходимо:\n"
									+ "💍 x2\n"
									+ "🎁 x60\n"
									+ "🔑 x15000");
							sender.RingCount -= 2;
							sender.GiftCount -= 60;
							await NadekoDbExtension.SetCurrencyValue(ctx.Member.Id, -15000);
							sender.MarriageTime = DateTimeOffset.UtcNow.AddDays(7);
							dbUser.MarriageTime = DateTimeOffset.UtcNow.AddDays(7);
							break;
						case "золотая":
							if (sender.RingCount < 2 || sender.GiftCount < 120 || senderCurrency < 30000)
								throw new ArgumentException("Для того чтобы сыграть Золотую свадьбу Вам необходимо:\n"
									+ "💍 x2\n"
									+ "🎁 x120\n"
									+ "🔑 x30.000");
							sender.RingCount -= 2;
							sender.GiftCount -= 120;
							await NadekoDbExtension.SetCurrencyValue(ctx.Member.Id, -30000);
							sender.MarriageTime = DateTimeOffset.UtcNow.AddDays(7);
							dbUser.MarriageTime = DateTimeOffset.UtcNow.AddDays(7);
							break;
						case "бриллиантовая":
							if (sender.RingCount < 2 || sender.GiftCount < 200 || senderCurrency < 50000)
								throw new ArgumentException("Для того чтобы сыграть Бриллиантовую свадьбу Вам необходимо:\n"
									+ "💍 x2\n"
									+ "🎁 x200\n"
									+ "🔑 x50.000");
							sender.RingCount -= 2;
							sender.GiftCount -= 200;
							await NadekoDbExtension.SetCurrencyValue(ctx.Member.Id, -50000);
							sender.MarriageTime = DateTimeOffset.UtcNow.AddDays(7);
							dbUser.MarriageTime = DateTimeOffset.UtcNow.AddDays(7);
							break;
						default:
							throw new ArgumentException($"Некорректно указан аргумент {ctx.Command.Arguments[0].Description}\nИспользуйте !свадьба [хрустальная/золотая/бриллиантовая] @user");
					}
					sender.PartnerId = targetUser.Id;
					dbUser.PartnerId = ctx.Member.Id;
					col.Update(dbUser);
					col.Update(sender);
				}
				embed.Description = $"{ctx.Member.Mention} {targetUser.Mention} Поздравляем, у вас назначена {marriageType} свадьба на {DateTime.UtcNow.AddDays(7).ToShortDateString()} {DateTime.UtcNow.AddDays(7).ToShortTimeString()} (МСК)";
				await ctx.RespondAsync(embed: embed);
			}
			catch (ArgumentException ex)
			{
				embed.Description = $"{ctx.Member.Mention} {ex.Message}";
				await ctx.RespondAsync(embed: embed);
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
			}
		}

		[Command("divorce"), Aliases("развод")]
		public async Task DivorceAsync(CommandContext ctx)
		{
			var embed = new DiscordEmbedBuilder
			{
				Author = new DiscordEmbedBuilder.EmbedAuthor
				{
					Name = $"💒 Развод",
				},
				Color = new DiscordColor("#321272"),
			};
			try
			{
				using (var db = new LiteDatabase($"filename={Bot.botConfig.GuildID}_voiceStat.db; journal=false;"))
				{
					var col = db.GetCollection<UserModel>("Users");
					var dbUser = col.FindOne(x => x.Id == ctx.Member.Id);
					if (dbUser == null)
						throw new ArgumentException("Вы не найдены в базе");
					if (dbUser.PartnerId == 0)
						throw new ArgumentException("Вы ни с кем не обручены");
					if (await NadekoDbExtension.GetCurrencyValue(ctx.Member.Id) < 3000)
						throw new ArgumentException("У вас недостаточно 🔑. Необходимо 3000 🔑");
					dbUser.MarriageTime = default;
					var partnerDbUser = col.FindOne(x => x.Id == dbUser.PartnerId);
					if (partnerDbUser == null)
						throw new ArgumentException("Партнер не найден в базе");
					dbUser.PartnerId = default;
					partnerDbUser.MarriageTime = default;
					partnerDbUser.PartnerId = default;
					await NadekoDbExtension.SetCurrencyValue(ctx.Member.Id, -3000);
					col.Update(dbUser);
					col.Update(partnerDbUser);
				}
				embed.Description = $"{ctx.Member.Mention} Вы разорвали брачные узы с Вашим партнером";
				await ctx.RespondAsync(embed: embed);
			}
			catch (ArgumentException ex)
			{
				embed.Description = $"{ctx.Member.Mention} {ex.Message}";
				await ctx.RespondAsync(embed: embed);
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
			}
		}

		[Command("kill"), Aliases("убить")]
		public async Task KillAsync(CommandContext ctx, DiscordMember targetUser = default)
		{
			var embed = new DiscordEmbedBuilder
			{
				Author = new DiscordEmbedBuilder.EmbedAuthor
				{
					Name = $"🔫 Покушение",
				},
				Color = new DiscordColor("#321272"),
			};
			try
			{
				await ctx.TriggerTypingAsync();
				if (targetUser == null)
					throw new ArgumentException("Некорректно указан аргумент");
				using (var db = new LiteDatabase($"filename={Bot.botConfig.GuildID}_voiceStat.db; journal=false;"))
				{
					var col = db.GetCollection<UserModel>("Users");
					var dbUser = col.FindOne(x => x.Id == ctx.Member.Id);
					if (dbUser == null)
						throw new ArgumentException("Вы не найдены в базе");
					if (dbUser.PistolCount == 0)
						throw new ArgumentException("Для покушения необходим хотя бы 1 🔫");
					if (dbUser.AmmoCount < 10)
						throw new ArgumentException($"Для покушения необходимо минимум 10 {DiscordEmoji.FromName(ctx.Client, ":bullet:")}");
					var targetDbUser = col.FindOne(x => x.Id == targetUser.Id);
					if (targetDbUser == null)
						throw new ArgumentException($"{targetUser.Username} не найден(а) в базе");
					if (targetDbUser.LastAssassinatedTime != null && (targetDbUser.LastAssassinatedTime.Value.AddHours(1) - DateTimeOffset.UtcNow).TotalSeconds > 1)
						throw new ArgumentException($"Следующее покушение на {targetUser.Mention} будет доступно через {(targetDbUser.LastAssassinatedTime.Value.AddHours(1) - DateTimeOffset.UtcNow).Hours} ч. {(targetDbUser.LastAssassinatedTime.Value.AddHours(1) - DateTimeOffset.UtcNow).Minutes} мин. {(targetDbUser.LastAssassinatedTime.Value.AddHours(1) - DateTimeOffset.UtcNow).Seconds} сек.");
					if (targetDbUser.SmallShieldEndTime != null && (targetDbUser.SmallShieldEndTime.Value - DateTimeOffset.UtcNow).TotalSeconds > 1)
						throw new ArgumentException($"Пользователь {targetUser.Mention} временно защищен от покушения малым щитом.\n"
						+ $"До окончания щита {(targetDbUser.SmallShieldEndTime.Value - DateTimeOffset.UtcNow).Days} д. {(targetDbUser.SmallShieldEndTime.Value - DateTimeOffset.UtcNow).Hours} ч. {(targetDbUser.SmallShieldEndTime.Value - DateTimeOffset.UtcNow).Minutes} мин.");
					targetDbUser.SmallShieldEndTime = null;
					var bulletsCost = Bot.random.Next(5, 10);
					dbUser.AmmoCount -= (uint)bulletsCost;
					col.Update(dbUser);
					if (targetDbUser.SuperShieldEndurance > 0)
					{
						targetDbUser.LastAssassinatedTime = DateTimeOffset.UtcNow;
						targetDbUser.SuperShieldEndurance--;
						col.Update(targetDbUser);
						throw new ArgumentException($"Вы использовали {bulletsCost} {DiscordEmoji.FromName(ctx.Client, ":bullet:")} для нападения на пользователя {targetUser.Mention}.\nНо Боевой щит защитил его от атаки.\n"
						+ $"Осталось прочности Боевоего щита: {targetDbUser.SuperShieldEndurance}");
					}
					var waifuCostDecresePer = Bot.random.Next(2, 10) / 100f;
					await NadekoDbExtension.SetWaifuCostValue(targetUser.Id, 1 - waifuCostDecresePer);
					targetDbUser.LastAssassinatedTime = DateTimeOffset.UtcNow;
					col.Update(targetDbUser);
					var chance = Bot.random.Next(0, 100);
					if (chance < 15)
					{
						dbUser.PistolCount--;
						col.Update(dbUser);
						chance = Bot.random.Next(0, 100);
						if (chance < 60 && targetDbUser.DragonCount > 0)
						{
							targetDbUser.DragonCount--;
							col.Update(targetDbUser);
							dbUser.DragonEggCount++;
							col.Update(dbUser);
							throw new ArgumentException($"Вы использовали {bulletsCost} {DiscordEmoji.FromName(ctx.Client, ":bullet:")} для нападения на пользователя {targetUser.Mention}.\nУ Вас сломался 1 🔫, но Вы убили дракона и получили 1 🥚 и сбросили стоимость {targetUser.Mention} на {waifuCostDecresePer * 100}%");
						}
						throw new ArgumentException($"Вы использовали {bulletsCost} {DiscordEmoji.FromName(ctx.Client, ":bullet:")} для нападения на пользователя {targetUser.Mention}.\nУ Вас сломался 1 🔫, но Вы сбросили стоимость {targetUser.Mention} на {waifuCostDecresePer * 100}%");
					}
					chance = Bot.random.Next(0, 100);
					if (chance < 10 && targetDbUser.DragonCount > 0)
					{
						targetDbUser.DragonCount--;
						col.Update(targetDbUser);
						dbUser.DragonEggCount++;
						col.Update(dbUser);
						throw new ArgumentException($"Вы использовали {bulletsCost} {DiscordEmoji.FromName(ctx.Client, ":bullet:")} для нападения на пользователя {targetUser.Mention}.\nВы получили 1 🥚, убив дракона и сбросили стоимость {targetUser.Mention} на {waifuCostDecresePer * 100}%");
					}
					embed.Description = $"{ctx.Member.Mention} Вы использовали {bulletsCost} {DiscordEmoji.FromName(ctx.Client, ":bullet:")} для нападения на пользователя {targetUser.Mention}.\nВы сбросили стоимость {targetUser.Mention} на {waifuCostDecresePer * 100}%";
					await ctx.RespondAsync(embed: embed);
				}
			}
			catch (ArgumentException ex)
			{
				embed.Description = $"{ctx.Member.Mention} {ex.Message}";
				await ctx.RespondAsync(embed: embed);
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
			}
		}

		[Command("shop"), Aliases("магазин")]
		public async Task ShopAsync(CommandContext ctx)
		{
			try
			{
				await ctx.TriggerTypingAsync();
				var embed = new DiscordEmbedBuilder
				{
					Author = new DiscordEmbedBuilder.EmbedAuthor
					{
						Name = "🛒 Магазин"
					},
					Color = new DiscordColor("#321272")
				};
				embed.AddField($"{DiscordEmoji.FromName(ctx.Client, ":bullet:")} Патрон", "100", true);
				embed.AddField("🎁 Пак", "150", true);
				embed.AddField("🛡 Малый щит (1 д.)", "1000", true);
				embed.AddField("🛡 Боевой щит (3-4 проч.)", "2000", true);
				embed.AddField("🎨 Краска", "1500", true);
				embed.AddField("🥚 Яйцо", "6000", true);
				embed.AddField("💍 Кольцо", "8000", true);
				embed.AddField("🔫 Пистолет", "9000", true);
				embed.AddField($"{DiscordEmoji.FromName(ctx.Client, ":incubator:")} Инкубатор", "15000", true);
				await ctx.RespondAsync(embed: embed);
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
			}
			return;
		}

		[Command("buy"), Aliases("купить")]
		public async Task BuyAsync(CommandContext ctx, uint value = 1, [Description("имя предмета")]string item = default)
		{
			var embed = new DiscordEmbedBuilder
			{
				Author = new DiscordEmbedBuilder.EmbedAuthor
				{
					Name = "🛒 Покупка"
				},
				Color = new DiscordColor("#321272")
			};
			try
			{
				await ctx.TriggerTypingAsync();
				if (string.IsNullOrEmpty(item))
					throw new ArgumentException($"Некорректно указан аргумент [{ctx.Command.Arguments[1].Description}]");
				using (var db = new LiteDatabase($"filename={Bot.botConfig.GuildID}_voiceStat.db; journal=false;"))
				{
					var col = db.GetCollection<UserModel>("Users");
					var dbUser = col.FindOne(x => x.Id == ctx.Member.Id);
					if (dbUser == null)
						throw new ArgumentException("Вы не найдены в базе");
					var keys = await NadekoDbExtension.GetCurrencyValue(ctx.User.Id);
					int price = default;
					DiscordEmoji itemEmoji = default;
					switch (item.ToLower())
					{
						case "патрон":
							if (keys < 100 * (int)value)
								throw new ArgumentException("У Вас недостаточно 🔑");
							dbUser.AmmoCount += value; ;
							price = 100 * (int)value;
							itemEmoji = DiscordEmoji.FromName(ctx.Client, ":bullet:");
							break;
						case "пак":
							if (keys < 150 * (int)value)
								throw new ArgumentException("У Вас недостаточно 🔑");
							dbUser.GiftCount += value; ;
							price = 150 * (int)value;
							itemEmoji = DiscordEmoji.FromName(ctx.Client, ":gift:");
							break;
						case "малыйщит":
							if (keys < 1000 * (int)value)
								throw new ArgumentException("У Вас недостаточно 🔑");
							dbUser.SmallShieldEndTime = dbUser.SmallShieldEndTime == null ? DateTimeOffset.UtcNow.AddDays(value) : dbUser.SmallShieldEndTime.Value.AddDays(value);
							price = 1000 * (int)value;
							itemEmoji = DiscordEmoji.FromName(ctx.Client, ":shield:");
							break;
						case "боевойщит":
							if (keys < 2000 * (int)value)
								throw new ArgumentException("У Вас недостаточно 🔑");
							var endurance = Bot.random.Next(3, 4);
							dbUser.SuperShieldEndurance += (uint)(endurance * value);
							price = 2000 * (int)value;
							itemEmoji = DiscordEmoji.FromName(ctx.Client, ":shield:");
							break;
						case "краска":
							if (keys < 1500 * (int)value)
								throw new ArgumentException("У Вас недостаточно 🔑");
							dbUser.PaintCount += value;
							price = 1500 * (int)value;
							itemEmoji = DiscordEmoji.FromName(ctx.Client, ":art:");
							break;
						case "яйцо":
							if (keys < 6000 * (int)value)
								throw new ArgumentException("У Вас недостаточно 🔑");
							dbUser.DragonEggCount += value;
							if (dbUser.EggRespawnTimes == null)
								dbUser.EggRespawnTimes = new List<DateTimeOffset>();
							for (int i = 0; i < value; i++)
							{
								dbUser.EggRespawnTimes.Add(DateTimeOffset.UtcNow.AddHours(18));
							}
							price = 6000 * (int)value;
							itemEmoji = DiscordEmoji.FromName(ctx.Client, ":egg:");
							break;
						case "кольцо":
							if (keys < 8000 * (int)value)
								throw new ArgumentException("У Вас недостаточно 🔑");
							dbUser.RingCount += value;
							price = 8000 * (int)value;
							itemEmoji = DiscordEmoji.FromName(ctx.Client, ":ring:");
							break;
						case "пистолет":
							if (keys < 9000 * (int)value)
								throw new ArgumentException("У Вас недостаточно 🔑");
							dbUser.PistolCount += value;
							price = 9000 * (int)value;
							itemEmoji = DiscordEmoji.FromName(ctx.Client, ":gun:");
							break;
						case "инкубатор":
							if (keys < 15000 * (int)value)
								throw new ArgumentException("У Вас недостаточно 🔑");
							dbUser.IncubatorCount += value;
							if (dbUser.IncubatorRespawnTimes == null)
								dbUser.IncubatorRespawnTimes = new List<DateTimeOffset>();
							for (int i = 0; i < value; i++)
							{
								dbUser.IncubatorRespawnTimes.Add(DateTimeOffset.UtcNow.AddHours(18));
							}
							price = 15000 * (int)value;
							itemEmoji = DiscordEmoji.FromName(ctx.Client, ":incubator:");
							break;
						default:
							embed.Description = $"{ctx.Member.Mention} Указанный предмет не существует в магазине";
							await ctx.RespondAsync(embed: embed);
							return;
					}
					col.Update(dbUser);
					await NadekoDbExtension.SetCurrencyValue(ctx.Member.Id, -price);
					embed.Description = $"{ctx.Member.Mention} Вы приобрели {value} {itemEmoji}";
				}
				await ctx.RespondAsync(embed: embed);
			}
			catch (ArgumentException ex)
			{
				embed.Description = $"{ctx.Member.Mention} {ex.Message}";
				await ctx.RespondAsync(embed: embed);
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
			}
		}

		private async Task<DiscordMessage> FindMessage(CommandContext ctx, ulong id)
		{
			DiscordMessage quoteMessage = null;
			var guildChannels = ctx.Guild.Channels;
			for (int i = -1; i < guildChannels.Count; i++)
			{
				try
				{
					if (i == -1)
						quoteMessage = await ctx.Channel.GetMessageAsync(id);
					else quoteMessage = await guildChannels[i].GetMessageAsync(id);
					return quoteMessage;
				}
				catch
				{
					continue;
				}
			}
			return quoteMessage;
		}
	}
}