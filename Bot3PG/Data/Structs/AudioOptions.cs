using Discord;

namespace Bot3PG.Data.Structs
{
    public struct TrackOptions
    {
        public string ID { get; set; }

        public int SkipVotes { get; set; }
        public bool Repeat { get; set; }
        public ulong RequestorID { get; set; }

        public TrackOptions(string id, ulong requestorID, bool repeat = false)
        {
            ID = id;
            RequestorID = requestorID;
            Repeat = repeat;
            SkipVotes = 0;
        }
    }
}