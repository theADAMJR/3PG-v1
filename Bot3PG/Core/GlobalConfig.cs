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
    public class GlobalConfig
    {
        public const string configFolder = "Resources";
        public const string configFile = "config.json";
        
        private static readonly Config _config;

        static GlobalConfig()
        {
            if (!Directory.Exists(configFolder))
                Directory.CreateDirectory(configFolder);

            if (!File.Exists(configFolder + "/" + configFile))
            {
                _config = new Config();
                string json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                File.WriteAllText(configFolder + "/" + configFile, json);
            }
            else
            {
                string json = File.ReadAllText(configFolder + "/" + configFile);
                _config = JsonConvert.DeserializeObject<Config>(json);
            }
            Global.Config = _config;
        }
    }
}