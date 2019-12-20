using System;
using System.Linq;
using System.IO;

namespace Bot3PG.Data
{
    public class BannedWords
    {
        private const string BanWordsFolder = "Resources";
        private const string BanWordsFile = "ban-words.txt";

        protected BannedWords()
        {
            if (!Directory.Exists(BanWordsFolder))
            {
                Directory.CreateDirectory(BanWordsFolder);
            }
            if (!File.Exists(BanWordsFolder + "/" + BanWordsFile))
            {
                File.Create(BanWordsFolder + "/" + BanWordsFile);
            }
        }

        public static string[] Words => File.ReadAllLines(BanWordsFolder + "/" + BanWordsFile).ToArray();
    }
}