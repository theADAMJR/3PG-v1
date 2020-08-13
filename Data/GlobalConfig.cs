using Bot3PG.Data.Structs;
using System.IO;
using Newtonsoft.Json;

namespace Bot3PG.Data
{
    public static class GlobalConfig
    {
        private const string configFolder = "Resources";
        private const string configFile = "config.json";

        private const string path = configFolder + "/" + configFile;

        public static Config Config { get; private set; }

        static GlobalConfig()
        {
            if (!Directory.Exists(configFolder))
                Directory.CreateDirectory(configFolder);
            
            if (!File.Exists(path))
            {
                Config = new Config();
                string json = JsonConvert.SerializeObject(Config, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            else
            {
                string json = File.ReadAllText(path);
                Config = JsonConvert.DeserializeObject<Config>(json);
            }
        }
    }
}