using System;
using System.Linq;

namespace Bot3PG.Utilities
{
    public static class CommandUtilities
    {
        public static TimeSpan ParseDuration(string duration)
        {
            if (string.IsNullOrEmpty(duration) || duration == "-1")
            {
                return TimeSpan.MaxValue;
            }
            var allLetters = duration.Where(c => char.IsLetter(c));
            string letters = string.Concat(allLetters);

            var allNumbers = duration.Where(c => char.IsNumber(c));
            string numberString = string.Concat(allNumbers);

            int.TryParse(numberString, out int time);

            switch (letters)
            {
                case string word when (word == "y" || word == "year"):
                    return TimeSpan.FromDays(365 * time);
                case string word when (word == "mo" || word == "month"):
                    return TimeSpan.FromDays(28 * time);
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
            throw new CommandException("Could not parse duration. Make sure you typed the duration correctly.");
        }
    }
}