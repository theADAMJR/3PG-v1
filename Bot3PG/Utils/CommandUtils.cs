using System;
using System.IO;
using System.Linq;
using System.Net;
using Discord.WebSocket;

namespace Bot3PG.Utils
{
    public static class CommandUtils
    {
        public static TimeSpan ParseDuration(string str)
        {
            if (string.IsNullOrEmpty(str) || str == "-1" || str.ToLower() == "forever") return TimeSpan.MaxValue;

            var allLetters = str.Where(c => char.IsLetter(c));
            string letters = string.Concat(allLetters);

            var allNumbers = str.Where(c => char.IsNumber(c));
            string numbers = string.Concat(allNumbers);

            int.TryParse(numbers, out int time);

            switch (letters)
            {
                case string word when (word == "y" || word == "year"):
                    return TimeSpan.FromDays(365 * time);
                case string word when (word == "mo" || word == "month"):
                    return TimeSpan.FromDays(30 * time);
                case string word when (word == "w" || word == "week"):
                    return TimeSpan.FromDays(7 * time);
                case string word when (word == "d" || word == "day"):
                    return TimeSpan.FromDays(time);
                case string word when (word == "h" || word == "hour"):
                    return TimeSpan.FromHours(time);
                case string word when (word == "m" || word == "min"):
                    return TimeSpan.FromMinutes(time);
                case string word when (word == "s" || word == "sec"):
                    return TimeSpan.FromSeconds(time);
            }
            throw new ArgumentException("Could not parse duration. Make sure you typed the duration correctly.");
        }

        public static string SetGuildVariables(string text, SocketGuildUser socketGuildUser)
        {
            if (socketGuildUser is null)
                throw new ArgumentNullException(nameof(socketGuildUser));

            text = text.Replace("[NICKNAME]", socketGuildUser.Nickname);
            text = text.Replace("[OWNER]", $"{socketGuildUser.Guild.Owner}");
            text = text.Replace("[USER]", socketGuildUser.Mention);
            text = text.Replace("[USER_COUNT]", $"{socketGuildUser.Guild.Users.Count}");
            text = text.Replace("[USERNAME]", socketGuildUser.Username);
            text = text.Replace("[SERVER]", socketGuildUser.Guild.Name);
            return text;
        }

        public static Stream DownloadData(string url)
        {
            try
            {
                var req = WebRequest.Create(url);
                var res = req.GetResponse();
                return res.GetResponseStream();
            }
            catch (Exception) { throw new Exception("There was a problem downloading the image file."); }
        }
    }
}