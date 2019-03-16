using System.Linq;
using System.IO;

namespace Bot3PG
{
    public class BannedLinks
    {
        private const string BanLinksFolder = "Resources";
        private const string BanLinksFile = "ban-links.txt";

        public BannedLinks()
        {
            if (!Directory.Exists(BanLinksFolder))
                Directory.CreateDirectory(BanLinksFolder);

            if (!File.Exists(BanLinksFolder + "/" + BanLinksFile))
            {
                File.Create(BanLinksFolder + "/" + BanLinksFile);
            }
        }

        static void AddLink(string word)
        {
            File.WriteAllText(BanLinksFile, word);
        }

        static void CheckLinks()
        {
            File.ReadAllLines(BanLinksFile);
        }

        public static string[] GetLinks()
        {
            return File.ReadAllLines(BanLinksFolder + "/" + BanLinksFile).ToArray();
        }
    }
}