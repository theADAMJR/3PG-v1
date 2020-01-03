using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Bot3PG.Utils
{
    public static class ExtensionMethods
    {
        public static string ToTitleCase(this string str)
        {
            var cultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            return cultureInfo.TextInfo.ToTitleCase(str.ToLower());
        }

        public static string ToTitleCase(this string str, string cultureInfoName)
        {
            var cultureInfo = new CultureInfo(cultureInfoName);
            return cultureInfo.TextInfo.ToTitleCase(str.ToLower());
        }

        public static string ToTitleCase(this string str, CultureInfo cultureInfo) => cultureInfo.TextInfo.ToTitleCase(str.ToLower());

        public static string ToSentenceCase(this string str)
        {
            var spaced = Regex.Replace(str, @"([A-Z][a-z])", " $0").ToCharArray();
            spaced[0] = spaced[0].ToString().ToUpper()[0];
            return String.Concat(spaced);
        }

        public static string ToTimestamp(this DateTime dateTime) => dateTime.ToString("dd/mm/yy hh:mm:ss");
    }
}