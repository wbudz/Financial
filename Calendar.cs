using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Financial
{
    public static class Calendar
    {
        /// <summary>
        /// Specifies time step for function involving time periods.
        /// </summary>
        public enum TimeStep
        {
            Daily,
            Weekly,
            Monthly,
            Quarterly,
            Semiannually,
            Yearly
        }

        /// <summary>
        /// Returns end date of the period containing specified date. Type of period is defined by step.
        /// </summary>
        /// <param name="date">Date which is inside of the period</param>
        /// <param name="step">Length (type) of period</param>
        /// <returns>Period end date</returns>
        public static DateTime GetEndDate(DateTime date, TimeStep step)
        {
            date = date.Date;
            switch (step)
            {
                case TimeStep.Daily:
                    return date;
                case TimeStep.Weekly:
                    while (date.DayOfWeek != DayOfWeek.Sunday)
                    {
                        date = date.AddDays(1);
                    }
                    return date;
                case TimeStep.Monthly:
                    return new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
                case TimeStep.Quarterly:
                    date = new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
                    while (date.Month % 3 != 0)
                    {
                        date = date.AddMonths(1);
                        date = new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
                    }
                    return date;
                case TimeStep.Semiannually:
                    date = new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
                    while (date.Month % 6 != 0)
                    {
                        date = date.AddMonths(1);
                        date = new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
                    }
                    return date;
                case TimeStep.Yearly:
                    return new DateTime(date.Year, 12, 31);
                default:
                    return date;
            }
        }

        public static DateTime AddAndAlignToEndDate(DateTime date, int steps, TimeStep step)
        {
            date = GetEndDate(date, step);
            switch (step)
            {
                case TimeStep.Daily:
                    return date.AddDays(steps);
                case TimeStep.Weekly:
                    return date.AddDays(steps * 7);
                case TimeStep.Monthly:
                    return GetEndDate(date.AddMonths(steps), step);
                case TimeStep.Quarterly:
                    return GetEndDate(date.AddMonths(steps * 3), step);
                case TimeStep.Semiannually:
                    return GetEndDate(date.AddMonths(steps * 6), step);
                case TimeStep.Yearly:
                    return date.AddYears(steps);
                default:
                    return date;
            }

        }

        /// <summary>
        /// Generate dates ending periods between provided start and end dates.
        /// </summary>
        /// <param name="start">Start date</param>
        /// <param name="end">End date</param>
        /// <param name="step">Length (type) of period</param>
        /// <returns>Dates ending periods</returns>
        /// <exception cref="ArgumentException">If provided start date is later than end date, exception is thrown.</exception>
        public static IEnumerable<DateTime> GenerateReportingDates(DateTime start, DateTime end, TimeStep step)
        {
            if (start > end) throw new ArgumentException("End date must fall later than start date.");
            DateTime date = start.Date;

            while (date <= end)
            {
                date = GetEndDate(date, step);
                if (date > end) yield break;
                yield return date;
                date = date.AddDays(1);
            }

        }

        /// <summary>
        /// Gets a number representing quarter of a year for a given date.
        /// </summary>
        /// <param name="date">Date</param>
        /// <returns>Quarter of a year</returns>
        private static int GetQuarter(DateTime date)
        {
            if (date.Month <= 3)
                return 1;
            else if (date.Month <= 6)
                return 2;
            else if (date.Month <= 9)
                return 3;
            else return 4;
        }

        /// <summary>
        /// Gets a number representing half of a year for a given date.
        /// </summary>
        /// <param name="date">Date</param>
        /// <returns>Half of a year</returns>
        private static int GetHalfYear(DateTime date)
        {
            if (date.Month <= 6)
                return 1;
            else
                return 2;
        }

        public static DateTime WorkingDays(DateTime start, long days, string locale = "")
        {
            if (String.IsNullOrEmpty(locale)) locale = CultureInfo.CurrentCulture.Name;
            if (days == 0)
            {
                while (!IsWorkingDay(start, locale))
                {
                    start = start.AddDays(1);
                }
                return start;
            }
            else if (days > 0)
            {
                while (days > 0)
                {
                    start = start.AddDays(1);
                    if (IsWorkingDay(start, locale)) days--;
                }
                return start;
            }
            else
            {
                while (days < 0)
                {
                    start = start.AddDays(-1);
                    if (IsWorkingDay(start, locale)) days++;
                }
                return start;
            }
        }

        public static bool IsWorkingDay(DateTime date, string locale = "")
        {
            if (String.IsNullOrEmpty(locale)) locale = CultureInfo.CurrentCulture.Name;
            return !IsHoliday(date, locale) && date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday;
        }

        public static bool IsHoliday(DateTime date, string locale = "")
        {
            if (String.IsNullOrEmpty(locale)) locale = CultureInfo.CurrentCulture.Name;
            return Holidays.IsHoliday(date, locale);
        }

    }
}
