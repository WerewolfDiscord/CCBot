using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Net.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CCBot
{
	//The Main Class From An Example.
	public class Bot : IDisposable
	{
		public const string n = "\r\n";

		public static DiscordClient _client;
		public static CommandsNextExtension _cnext;
		private readonly CancellationTokenSource _cts;
		private readonly StartTimes _starttimes;
		private Config _config;
		private InteractivityExtension _interactivity;

		public Bot()
		{
			var path = "config.json";
			path = Path.GetFullPath(path);
			if (!File.Exists(path))
			{
				if (!Directory.Exists(Path.GetDirectoryName(path)))
					Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new DirectoryNotFoundException());
				new Config().SaveToFile(path);

				#region !! Report to user that config has not been set yet !! (aesthetics)

				Console.BackgroundColor = ConsoleColor.Red;
				Console.ForegroundColor = ConsoleColor.Black;
				WriteCenter("▒▒▒▒▒▒▒▒▒▄▄▄▄▒▒▒▒▒▒▒", 2);
				WriteCenter("▒▒▒▒▒▒▄▀▀▓▓▓▀█▒▒▒▒▒▒");
				WriteCenter("▒▒▒▒▄▀▓▓▄██████▄▒▒▒▒");
				WriteCenter("▒▒▒▄█▄█▀░░▄░▄░█▀▒▒▒▒");
				WriteCenter("▒▒▄▀░██▄░░▀░▀░▀▄▒▒▒▒");
				WriteCenter("▒▒▀▄░░▀░▄█▄▄░░▄█▄▒▒▒");
				WriteCenter("▒▒▒▒▀█▄▄░░▀▀▀█▀▒▒▒▒▒");
				WriteCenter("▒▒▒▄▀▓▓▓▀██▀▀█▄▀▀▄▒▒");
				WriteCenter("▒▒█▓▓▄▀▀▀▄█▄▓▓▀█░█▒▒");
				WriteCenter("▒▒▀▄█░░░░░█▀▀▄▄▀█▒▒▒");
				WriteCenter("▒▒▒▄▀▀▄▄▄██▄▄█▀▓▓█▒▒");
				WriteCenter("▒▒█▀▓█████████▓▓▓█▒▒");
				WriteCenter("▒▒█▓▓██▀▀▀▒▒▒▀▄▄█▀▒▒");
				WriteCenter("▒▒▒▀▀▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒");
				Console.BackgroundColor = ConsoleColor.Yellow;
				WriteCenter("WARNING", 3);
				Console.ResetColor();
				WriteCenter("Thank you Mario!", 1);
				WriteCenter("But our config.json is in another castle!");
				WriteCenter("(Please fill in the config.json that was generated.)", 2);
				WriteCenter($"({path})");
				WriteCenter("Press any key to exit..", 1);
				Console.SetCursorPosition(0, 0);
				Console.ReadKey();

				#endregion

				Environment.Exit(0);
			}
			_config = Config.LoadFromFile(path);
			_client = new DiscordClient(new DiscordConfiguration
			{
				AutoReconnect = true,
				LogLevel = LogLevel.Debug,
				Token = _config.Token,
				TokenType = TokenType.Bot,
				UseInternalLogHandler = true,
				AutomaticGuildSync = true,
				WebSocketClientFactory = WebSocket4NetCoreClient.CreateNew
			});

			_interactivity = _client.UseInteractivity(new InteractivityConfiguration
			{
				PaginationBehavior = TimeoutBehaviour.DeleteMessage,
				PaginationTimeout = TimeSpan.FromSeconds(30),
				Timeout = TimeSpan.FromSeconds(30)
			});

			_starttimes = new StartTimes
			{
				BotStart = DateTime.UtcNow,
				SocketStart = DateTime.MinValue
			};

			_cts = new CancellationTokenSource();


            try
            {
                var v = File.ReadAllText("Settings.json");
                Dependencies.Settings = Settings.FromJson(v);
                Console.WriteLine($"Loaded {Dependencies.Settings.SignedUpPlayers.Count} Players and {Dependencies.Settings.Channels.Count} Channels");
            }
            catch (Exception ex)
            {
                Dependencies.Settings = new Settings();
                if (File.Exists("Settings.json"))
                {
                    File.Copy("Settings.json", "Settings.json.backup", true);
                    Console.WriteLine("Failed to Load. Created backup");
                }
                else
                    Console.WriteLine("Failed to load.");
            }

            Dependencies.Settings.Validate();

            var dep = new ServiceCollection()
                .AddSingleton(new Dependencies
                {
					Interactivity = _interactivity,
					StartTimes = _starttimes,
					Cts = _cts
				}).BuildServiceProvider();

			_cnext = _client.UseCommandsNext(new CommandsNextConfiguration
			{
				CaseSensitive = false,
				EnableDefaultHelp = true,
				EnableDms = true,
				EnableMentionPrefix = true,
				StringPrefixes = new List<string>()
				{
					_config.Prefix
				},
				IgnoreExtraArguments = true,
				Services = dep
			});

			_cnext.RegisterCommands<Main>();

			_client.Ready += OnReadyAsync;
			_client.SocketClosed += _client_SocketClosed;
			_cnext.CommandErrored += _cnext_CommandErrored;
			_client.DebugLogger.LogMessageReceived += DebugLogger_LogMessageReceived;
			_cnext.CommandExecuted += _cnext_CommandExecuted;
			//Console.Write(Default3);
		}

		public void Dispose()
		{
			_client.Dispose();
			_interactivity = null;
			_cnext = null;
			_config = null;
		}

		private static async Task _cnext_CommandExecuted(CommandExecutionEventArgs e)
		{
            await e.Context.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("👌"));
        }

		private static void DebugLogger_LogMessageReceived(object sender, DebugLogMessageEventArgs e)
		{
			if (e.Application != "REST")
			{
				//GameLog.Log($"[{e.Application}] [{e.Level}] {e.Message}", "Logger");
			}
		}

		private static async Task _cnext_CommandErrored(CommandErrorEventArgs e)
		{
			//await e.Context.Channel.SendMessageAsync(
			//    $"{e.Context.Message.Author.Mention}'s Command ```{Environment.NewLine}{e.Context.Message.Content}{Environment.NewLine}``` ERRORED SEE LOG");
			//await e.Context.Message.DeleteAsync();
			Console.WriteLine($"ERROR: {e.Command}, {e.Exception.Message}");
            //GameLog.Log($"[ERROR] {e.Command}, {e.Exception.Message}", "Commands");
            await e.Context.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("❎"));
        }

		private static Task _client_SocketClosed(SocketCloseEventArgs e)
		{
			return Task.Delay(0);
		}

		public async Task RunAsync()
		{
			await _client.ConnectAsync();
			await WaitForCancellationAsync();
		}

		private async Task WaitForCancellationAsync()
		{
            while (!_cts.IsCancellationRequested)
            {
                File.WriteAllText("Settings.json", Dependencies.Settings.ToJson());
                Console.WriteLine("Saved.");
                await Task.Delay(60000);
            }
		}

		private async Task OnReadyAsync(ReadyEventArgs e)
		{
			await Task.Yield();
			_starttimes.SocketStart = DateTime.UtcNow;
		}

		private static void WriteCenter(string value, int skipline = 0)
		{
			for (var i = 0; i < skipline; i++)
				Console.WriteLine();

			Console.SetCursorPosition((Console.WindowWidth - value.Length) / 2, Console.CursorTop);
			Console.WriteLine(value);
		}
	}
}