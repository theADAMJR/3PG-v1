using System.Linq;
using System.IO;

namespace Bot3PG.Data
{
    public class BannedLinks
    {
        private const string BanLinksFolder = "Resources";
        private const string BanLinksFile = "ban-links.txt";

        protected BannedLinks()
        {
            if (!Directory.Exists(BanLinksFolder))
            {
                Directory.CreateDirectory(BanLinksFolder);
            }
            if (!File.Exists(BanLinksFolder + "/" + BanLinksFile))
            {
                File.Create(BanLinksFolder + "/" + BanLinksFile);
            }
        }

        public static string[] Links => File.ReadAllLines(BanLinksFolder + "/" + BanLinksFile).ToArray();
    }
}