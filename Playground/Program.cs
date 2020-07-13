﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Playground.Exceptions;

namespace Playground
{
    class Program
    {
        public static Game Game = new Game();
        public static Random Random = new Random();

        public static void Main()
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            const string token = "";
            var client = await InitClient(token);
            Game.PlayerKicked += (sender, args) => Console.WriteLine($"{args.Player.User.Username} kicked");
            await Task.Delay(-1);
        }


        private static async Task ClientOnMessageReceived(SocketMessage arg, DiscordSocketClient client)
        {
            var mentioned = arg.MentionedUsers.Any(u => u.Id == client.CurrentUser.Id);
            var author = arg.Author;
            if (author.Id == client.CurrentUser.Id) return;
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
                            await channel.SendMessageAsync($"🥶I tell you this, for when 👉 <@{Game.Next().User.Id}>'s days have come to and end. You, shall be king (🎲)");
                            return;
                        }
                        if (player.User.Id == author.Id)
                        {
                            await channel.SendMessageAsync($"🎲 (0 - {limit})\n<@{author.Id}> rolled {player.Roll}\n<@{Game.Next().User.Id}> is next");
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
            }
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

        private static IMessageChannel GetChannel(DiscordSocketClient client)
        {
            var guild = client.Guilds.FirstOrDefault(g => g.Name.ToLower().Contains("base"));
            if (guild is null)
            {
                return null;
            }

            var channel = guild.Channels.Single(guildChannel => guildChannel.Name == "bot");
            var channelId = channel.Id;
            if (channelId > 0)
            {
                return client.GetChannel(channelId) as IMessageChannel;
            }

            return null;
        }

        private async Task<DiscordSocketClient> InitClient(string token)
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