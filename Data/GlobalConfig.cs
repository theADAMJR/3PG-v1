using Bot3PG.Data.Structs;
using System.IO;
using Newtonsoft.Json;

namespace Bot3PG.Data
{
    public static class GlobalConfig
    {
        private const string configFile = "config.json";

        public static Config Config { get; private set; }

        static GlobalConfig()
        {            
            if (!File.Exists(configFile))
            {
                Config = new Config();
                string json = JsonConvert.SerializeObject(Config, Formatting.Indented);
                File.WriteAllText(configFile, json);
            }
            else
            {
                string json = File.ReadAllText(configFile);
                Config = JsonConvert.DeserializeObject<Config>(json);
            }
        }
    }
}