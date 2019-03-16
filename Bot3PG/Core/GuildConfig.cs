using Bot3PG.DataStructs;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace Bot3PG
{
    public class GuildConfig
    {
        public const string configFolder = "Resources";
        public const string configFile = "guildconfig.json";
        
        private static readonly Guild _config;

        static GuildConfig()
        {
            if (!Directory.Exists(configFolder))
                Directory.CreateDirectory(configFolder);

            if (!File.Exists(configFolder + "/" + configFile))
            {
                _config = new Guild();
                string json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                File.WriteAllText(configFolder + "/" + configFile, json);
            }
            else
            {
                string json = File.ReadAllText(configFolder + "/" + configFile);
                _config = JsonConvert.DeserializeObject<Guild>(json);
            }
        }
    }
}