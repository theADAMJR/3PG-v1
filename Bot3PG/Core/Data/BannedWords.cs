using System;
using System.Linq;
using System.IO;

namespace Bot3PG
{
    public class BannedWords
    {
        private const string BanWordsFolder = "Resources";
        private const string BanWordsFile = "ban-words.txt";

        public BannedWords()
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

        public static string[] GetWords() => File.ReadAllLines(BanWordsFolder + "/" + BanWordsFile).ToArray();
    }
}