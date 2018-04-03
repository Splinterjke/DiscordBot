using Serilog;
using Serilog.Events;
using System;
using System.Threading.Tasks;

namespace VoiceOnlineBot
{
	class Program
	{
		static void Main(string[] args)
		{
			Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Debug()
			.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
			.Enrich.FromLogContext()
			.WriteTo.Console()
			.CreateLogger();
			new Bot().Run().GetAwaiter().GetResult();
		}
	}
}
