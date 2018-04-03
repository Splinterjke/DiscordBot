using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;
using System.Threading.Tasks;

namespace VoiceOnlineBot
{
	public class NadekoDbExtension
	{
		public static async Task<Int64> GetWaifuCostValue(ulong UserdId)
		{
			try
			{
				using (SQLiteConnection connection = new SQLiteConnection($@"Data Source={Bot.botConfig.NadekoDBPath};Version=3;"))
				{
					using (SQLiteCommand cmd = new SQLiteCommand($"SELECT Price FROM WaifuInfo WHERE WaifuId = (SELECT Id FROM DiscordUser WHERE UserId = {UserdId} LIMIT 1)", connection))
					{
						await connection.OpenAsync();
						using (var reader = await cmd.ExecuteReaderAsync())
						{
							while (await reader.ReadAsync())
							{
								return (Int64)reader["Price"];
							}
						}
					}
				}
				return 0;
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
				return 0;
			}
		}

		public static async Task<bool> SetWaifuCostValue(ulong UserdId, float amount)
		{
			try
			{
				var value = amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
				using (SQLiteConnection connection = new SQLiteConnection($@"Data Source={Bot.botConfig.NadekoDBPath};Version=3;"))
				{
					using (SQLiteCommand cmd = new SQLiteCommand($"UPDATE WaifuInfo SET Price = round(Price * {value}) WHERE WaifuId = (SELECT Id FROM DiscordUser WHERE UserId = {UserdId} LIMIT 1)", connection))
					{
						await connection.OpenAsync();
						var rows = await cmd.ExecuteNonQueryAsync();
						return rows == 0 ? false : true;
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
				return false;
			}
		}

		public static async Task<Int64> GetCurrencyValue(ulong UserdId)
		{
			try
			{
				using (SQLiteConnection connection = new SQLiteConnection($@"Data Source={Bot.botConfig.NadekoDBPath};Version=3;"))
				{
					using (SQLiteCommand cmd = new SQLiteCommand($"SELECT CurrencyAmount FROM DiscordUser WHERE UserId = {UserdId}", connection))
					{
						await connection.OpenAsync();
						using (var reader = await cmd.ExecuteReaderAsync())
						{
							while (await reader.ReadAsync())
							{
								return (Int64)reader["CurrencyAmount"];
							}
						}
					}
				}
				return 0;
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
				return 0;
			}
		}

		public static async Task<bool> SetCurrencyValue(ulong UserdId, int amount)
		{
			try
			{
				using (SQLiteConnection connection = new SQLiteConnection($@"Data Source={Bot.botConfig.NadekoDBPath};Version=3;"))
				{
					using (SQLiteCommand cmd = new SQLiteCommand($"UPDATE DiscordUser SET CurrencyAmount = CurrencyAmount + {amount} WHERE UserId = {UserdId}", connection))
					{
						await connection.OpenAsync();
						var rows = await cmd.ExecuteNonQueryAsync();
						return rows == 0 ? false : true;
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
				return false;
			}
		}
	}
}
