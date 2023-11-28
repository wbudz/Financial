using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Financial
{
    internal static class Holidays
    {
        static Dictionary<(string locale, int year), HashSet<DateTime>> data = new();

        internal static bool IsHoliday(DateTime date, string locale)
        {
            if (!data.ContainsKey((locale, date.Year))) Generate(locale, date.Year);
            return data[(locale, date.Year)].Contains(date);
        }

        private static void Generate(string locale, int year)
        {
            HashSet<DateTime> hs = new HashSet<DateTime>();

            // Poland
            if (locale.StartsWith("pl"))
            {
                // New Year
                hs.Add(new DateTime(year, 1, 1));
                // Epiphany
                hs.Add(new DateTime(year, 1, 6));
                // Easter
                var easter = GetEasterDate(year);
                hs.Add(easter);
                hs.Add(easter.AddDays(1));
                // Labor Day
                hs.Add(new DateTime(year, 5, 1));
                // Constitution Day
                hs.Add(new DateTime(year, 5, 3));
                // Pentecost
                hs.Add(easter.AddDays(49));
                // Corpus Christi
                hs.Add(easter.AddDays(60));
                // Assumption of Mary
                hs.Add(new DateTime(year, 8, 15));
                // All Saint's Day
                hs.Add(new DateTime(year, 11, 1));
                // Independence Day
                hs.Add(new DateTime(year, 11, 11));
                // Christmas
                hs.Add(new DateTime(year, 12, 25));
                hs.Add(new DateTime(year, 12, 26));
            }
            else
            {
                throw new NotImplementedException();
            }

            data[(locale, year)] = hs;
        }

        private static DateTime GetEasterDate(int year)
        {
            // As given by ChatGPT

            int a = year % 19;
            int b = year / 100;
            int c = year % 100;
            int d = b / 4;
            int e = b % 4;
            int f = (b + 8) / 25;
            int g = (b - f + 1) / 3;
            int h = (19 * a + b - d - g + 15) % 30;
            int i = c / 4;
            int k = c % 4;
            int l = (32 + 2 * e + 2 * i - h - k) % 7;
            int m = (a + 11 * h + 22 * l) / 451;
            int month = (h + l - 7 * m + 114) / 31;
            int day = ((h + l - 7 * m + 114) % 31) + 1;

            return new DateTime(year, month, day);
        }

    }
}
