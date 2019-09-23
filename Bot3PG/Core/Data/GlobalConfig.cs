using Bot3PG.DataStructs;
using System.IO;
using Newtonsoft.Json;

namespace Bot3PG
{
    public static class GlobalConfig
    {
        public const string configFolder = "Resources";
        public const string configFile = "config.json";

        public static Config Config { get; private set; }

        static GlobalConfig()
        {
            if (!Directory.Exists(configFolder))
            {
                Directory.CreateDirectory(configFolder);
            }
            if (!File.Exists(configFolder + "/" + configFile))
            {
                Config = new Config();
                string json = JsonConvert.SerializeObject(Config, Formatting.Indented);
                File.WriteAllText(configFolder + "/" + configFile, json);
            }
            else
            {
                string json = File.ReadAllText(configFolder + "/" + configFile);
                Config = JsonConvert.DeserializeObject<Config>(json);
            }
        }
    }
}