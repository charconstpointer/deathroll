using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Discord.WebSocket;

namespace Playground
{
    public class Game
    {
        private readonly Queue<SocketUser> _players;
        private readonly Random _random;
        public int Limit { get; private set; }

        public IReadOnlyCollection<SocketUser> Players => _players.ToImmutableList();

        public SocketUser Next()
        {
            return !_players.Any() ? null : _players.Peek();
        }
        public bool IsStarted { get; private set; }

        public Game()
        {
            _players = new Queue<SocketUser>();
            _random = new Random();
            Limit = 1000000;
        }

        public void AddPlayer(SocketUser socketUser)
        {
            var alreadyAdded = _players.Contains(socketUser);
            if (!alreadyAdded)
            {
                _players.Enqueue(socketUser);
            }
        }

        public (SocketUser, int) Roll(SocketUser user)
        {
            var expected = _players.Peek();
            if (expected.Id != user.Id) return (null, 1);
            if (!_players.TryDequeue(out var player)) throw new ApplicationException("No players?");
            var roll = _random.Next(Limit);
            Limit = roll;
            _players.Enqueue(player);
            return (player, roll);
        }

        public void Start()
        {
            if (!IsStarted) IsStarted = true;
        }
    }
}