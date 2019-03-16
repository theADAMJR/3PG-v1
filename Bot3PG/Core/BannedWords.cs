using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace Bot3PG
{
    public class BannedWords
    {
        private const string BanWordsFolder = "Resources";
        private const string BanWordsFile = "ban-words.txt";

        public BannedWords()
        {
            if (!Directory.Exists(BanWordsFolder))
                Directory.CreateDirectory(BanWordsFolder);

            if (!File.Exists(BanWordsFolder + "/" + BanWordsFile))
            {
                File.Create(BanWordsFolder + "/" + BanWordsFile);
            }
        }

        static void AddBanWord(string word)
        {
            File.AppendAllText(BanWordsFolder + "/" + BanWordsFile, $"{word}{Environment.NewLine}");
        }

        static void CheckBadWords()
        {
            File.ReadAllLines(BanWordsFile);
        }

        public static string[] GetWords()
        {
            return File.ReadAllLines(BanWordsFolder + "/" + BanWordsFile).ToArray();
        }
    }
}