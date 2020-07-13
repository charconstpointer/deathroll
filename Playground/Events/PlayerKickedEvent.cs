using System;

namespace Playground.Events
{
    public class PlayerKickedEvent : EventArgs
    {
        public Player Player { get; set; }
    }
}