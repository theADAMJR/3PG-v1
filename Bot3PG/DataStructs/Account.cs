using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot3PG.DataStructs
{
    public class Account
    {
        public ulong ID { get; set; }

        public ulong GuildID { get; set; }

        public DateTime LastXPMsg { get; set; }

        public uint Points { get; set; }

        public uint XP { get; set; }

        public uint LevelNumber
        {
            get
            {
                return (uint)Math.Sqrt(XP / 100) + 1;
            }
        }

        public bool AgreedToRules { get; set; }

        public bool IsMuted { get; set; }

        public bool IsBanned { get; set; }

        public uint NumberOfWarnings { get; set; }
    }
}