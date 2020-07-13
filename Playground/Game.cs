using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Discord.WebSocket;

namespace Playground
{
    public class Game
    {
        private readonly Queue<Player> _players;
        private readonly Random _random;
        private readonly int _limit;
        public IReadOnlyCollection<Player> Players => _players.ToImmutableList();
        public int PlayerCount => _players.Count;
        private int _rolls = 0;

        public Player Next()
        {
            return !_players.Any() ? null : _players.Peek();
        }
        public bool IsStarted { get; private set; }

        public Game()
        {
            _players = new Queue<Player>();
            _random = new Random();
            _limit = 100000;
        }

        public void AddPlayer(Player socketUser)
        {
            var alreadyAdded = _players.Contains(socketUser);
            if (!alreadyAdded)
            {
                _players.Enqueue(socketUser);
            }
        }

        public Player Roll(SocketUser user)
        {
            var expected = _players.Peek();
            if (expected.User.Id != user.Id) return null;
            if (!_players.TryDequeue(out var player)) throw new ApplicationException("No players?");
            var roll = _random.Next(_limit);
            player.Roll = roll;
            _rolls++;
            _players.Enqueue(player);
            if (_rolls % PlayerCount == 0)
            {
                Console.WriteLine("full round");
            }

            return player;
        }

        // private SocketUser KickLoser()
        // {
        //     _players.
        // }
        
        public void Start()
        {
            if (!IsStarted) IsStarted = true;
        }
    }
}