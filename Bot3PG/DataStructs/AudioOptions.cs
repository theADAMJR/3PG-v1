using Discord;

namespace Bot3PG.DataStructs
{
    public struct AudioOptions
    {
        public bool Shuffle { get; set; }
        public bool RepeatTrack { get; set; }
        public IUser Summoner { get; set; }
    }
}