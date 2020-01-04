using System.Linq;
using System.IO;

namespace Bot3PG.Data
{
    public class BannedWords
    {
        private const string Folder = "Resources";
        private const string BadLinksFile = "ban-links.txt";
        private const string BadWordsFile = "ban-words.txt";

        public static string[] Links => File.ReadAllLines(Folder + "/" + BadLinksFile).ToArray();
        public static string[] Words => File.ReadAllLines(Folder + "/" + BadWordsFile).ToArray();

        protected BannedWords()
        {
            if (!Directory.Exists(Folder))
            {
                Directory.CreateDirectory(Folder);
            }
            if (!File.Exists(Folder + "/" + BadLinksFile))
            {
                File.Create(Folder + "/" + BadLinksFile);
            }
        }
    }
}