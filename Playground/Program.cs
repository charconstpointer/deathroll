﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Playground.Events;
using Playground.Exceptions;

namespace Playground
{
    class Program
    {
        private static readonly Game Game = new Game();

        public static void Main(string[] args)
            => MainAsync(args).GetAwaiter().GetResult();

        private static async Task MainAsync(IEnumerable<string> args)
        {
            var token = args.First();
            var client = await InitClient(token);
            var guild = client.Guilds.First(g => g.Name.ToLower().Contains("base"));
            var channel = guild.Channels.First(c => c.Name.ToLower().Contains("general"));
            var channelId = channel.Id;
            var cc = client.GetChannel(channelId) as IMessageChannel;
            if (cc is null) throw new ApplicationException($"Can't get #{channelId} channel");
            Game.PlayerKicked += async (sender, playerKickedEvent) =>
                await cc.SendMessageAsync($"<@{playerKickedEvent.Player.User.Id}> lost 🙀");

            async void OnGameOnGameEnded(object sender, GameEndedEvent gameEndedEvent)
            {
                await cc.SendMessageAsync($"<@{gameEndedEvent.Winner.User.Id}> is da winna 🎆🎇🎈✨🎉🎊");
                Game.Restart();
            }

            Game.GameEnded += OnGameOnGameEnded;
            await Task.Delay(-1);
        }

        private static async Task ClientOnMessageReceived(SocketMessage arg, DiscordSocketClient client)
        {
            var mentioned = VerifyReceiver(arg, client.CurrentUser.Id);
            var author = arg.Author;
            var channelId = arg.Channel.Id;
            if (!(client.GetChannel(channelId) is IMessageChannel channel)) return;
            if (mentioned)
            {
                if (arg.Content.ToLower().Contains("players"))
                {
                    var players = new StringBuilder();
                    var next = Game.Next();
                    foreach (var gamePlayer in Game.Players)
                    {
                        players.AppendLine(gamePlayer.User.Id == next.User.Id
                            ? $"👉 {gamePlayer.User.Username}"
                            : $"⏳ {gamePlayer.User.Username}");
                    }

                    await channel.SendMessageAsync(players.ToString());
                }
                else if (arg.Content.ToLower().Contains("join"))
                {
                    if (Game.IsStarted)
                    {
                        return;
                    }

                    var player = new Player {User = author, Roll = -1};
                    var added = Game.AddPlayer(player);
                    if (added) await channel.SendMessageAsync($"<@{author.Id}> Added");
                    else await channel.SendMessageAsync($"<@{author.Id}> You are already added, 🤦‍♂️🤦‍♀️");
                }
                else if (arg.Content.ToLower().Contains("roll"))
                {
                    var limit = Game.Limit;
                    try
                    {
                        var player = Game.Roll(author);
                        if (player is null)
                        {
                            await channel.SendMessageAsync(
                                $"🥶I tell you this, for when 👉 <@{Game.Next().User.Id}>'s days have come to and end. You, shall be king (🎲)");
                            return;
                        }

                        if (player.User.Id == author.Id)
                        {
                            await channel.SendMessageAsync(
                                $"🎲 (0 - {limit})\n<@{author.Id}> rolled {player.Roll}\n<@{Game.Next().User.Id}> is next");
                        }

                        if (player.Roll == 0)
                        {
                            await channel.SendMessageAsync($"<@{author.Id}> :( przegryw");
                        }
                    }
                    catch (GameOverException e)
                    {
                        await channel.SendMessageAsync($"⛔ 👉 🚪");
                    }
                }
                else if (arg.Content.ToLower().Contains("rules"))
                {
                    await channel.SendMessageAsync(
                        "🎢 Kazdy z graczy musi wylosowac liczbe za pomoca komendy roll\n🪂 Gracz z najslabszym rollem odpada\n🏁 Gdy zostanie tylko dwoch graczy zaczyna sie faza finalowa, rollujemy az ktos uzyska 0, kazdy roll, ktory jest wiekszy od 0, zmniejsza maksymalna wartosc rolla dla przeciwnika");
                }
            }
        }

        private static bool VerifyReceiver(SocketMessage message, ulong clientId)
        {
            var mentioned = message.MentionedUsers.Any(u => u.Id == clientId);
            var ownMessage = message.Author.Id == clientId;
            return mentioned && !ownMessage;
        }

        private static Task Log(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Console.WriteLine(
                $"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}");
            Console.ResetColor();
            return Task.CompletedTask;
        }

        private static async Task<DiscordSocketClient> InitClient(string token)
        {
            var client = new DiscordSocketClient();
            var commands = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Info,
                CaseSensitiveCommands = false,
            });
            commands.Log += Log;
            client.Log += Log;
            client.MessageReceived += async s => await ClientOnMessageReceived(s, client);
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();
            while (client.ConnectionState != ConnectionState.Connected)
            {
                await Task.Delay(1000);
            }

            return client;
        }
    }
}