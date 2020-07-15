using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Discord.WebSocket;
using Playground.Events;
using Playground.Exceptions;

namespace Playground
{
    public class Game
    {
        private Queue<Player> _players;
        private readonly Random _random;
        public IReadOnlyCollection<Player> Players => _players.ToImmutableList();
        public int PlayerCount => _players.Count;
        private int _rolls;
        public int Limit { get; private set; }

        public Player Next()
        {
            return !_players.Any() ? null : _players.Peek();
        }

        public bool IsStarted { get; private set; }
        public event EventHandler<PlayerKickedEvent> PlayerKicked;
        public event EventHandler<GameEndedEvent> GameEnded;

        public Game()
        {
            _players = new Queue<Player>();
            _random = new Random();
            Limit = 1000;
        }

        public bool AddPlayer(Player socketUser)
        {
            var alreadyAdded = _players.Any(p => p.User.Id == socketUser.User.Id);
            if (alreadyAdded) return false;
            _players.Enqueue(socketUser);
            return true;
        }

        public Player Roll(SocketUser user)
        {
            if (Limit == 0)
            {
                throw new GameOverException();
            }

            var expected = _players.Peek();
            if (expected.User.Id != user.Id) return null;
            var roll = _random.Next(Limit+1);
            if (PlayerCount <= 2)
            {
                Limit = roll;
                if (roll == 0)
                {
                    GameEnded?.Invoke(this, new GameEndedEvent
                    {
                        Winner = _players.First(p => p.User.Id != expected.User.Id)
                    });
                }
            }

            if (!_players.TryDequeue(out var player)) throw new ApplicationException("No players?");
            player.Roll = roll;
            _rolls++;
            _players.Enqueue(player);
            if (_rolls % PlayerCount != 0 || PlayerCount <= 2) return player;
            var loser = _players.ToList().OrderBy(p => p.Roll).First();
            KickLoser(loser);

            return player;
        }

        public void Restart()
        {
            _players = new Queue<Player>();
            Limit = 1000;
            _rolls = 0;
        }

        private void KickLoser(Player player)
        {
            var players = _players.ToList();
            players.Remove(player);
            var updatedPlayers = new Queue<Player>();
            foreach (var p in players)
            {
                updatedPlayers.Enqueue(p);
            }

            if (updatedPlayers.Count == 1)
            {
                GameEnded?.Invoke(this, new GameEndedEvent
                {
                    Winner = updatedPlayers.First()
                });
            }

            _players = updatedPlayers;
            PlayerKicked?.Invoke(this, new PlayerKickedEvent
            {
                Player = player
            });
        }

        public void Start()
        {
            if (!IsStarted) IsStarted = true;
        }
    }
}