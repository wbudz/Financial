using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Financial
{
    public enum DayCountConvention { US_30_360, Actual_Actual_Excel, Actual_360, Actual_365, European_30_360 }

    public enum EndOfMonthConvention { Align, DontAlign }

    public static class DayCount
    {
        static bool isShorterThanYear(DateTime date1, DateTime date2)
        {
            var d1d = date1 < date2 ? date1.Day : date2.Day;
            var d2d = date1 < date2 ? date2.Day : date1.Day;
            var d1m = date1 < date2 ? date1.Month : date2.Month;
            var d2m = date1 < date2 ? date2.Month : date1.Month;
            var d1y = date1 < date2 ? date1.Year : date2.Year;
            var d2y = date1 < date2 ? date2.Year : date1.Year;
            if (d1y == d2y) return true;
            if (d1y + 1 == d2y && (d1m > d2m) || (d1m == d2m && d1d >= d2d)) return true;
            return false;
        }

        static bool contains29Feb(DateTime date1, DateTime date2)
        {
            var d1d = date1 < date2 ? date1.Day : date2.Day;
            var d2d = date1 < date2 ? date2.Day : date1.Day;
            var d1m = date1 < date2 ? date1.Month : date2.Month;
            var d2m = date1 < date2 ? date2.Month : date1.Month;
            var d1y = date1 < date2 ? date1.Year : date2.Year;
            var d2y = date1 < date2 ? date2.Year : date1.Year;
            if (!isShorterThanYear(date1, date2)) throw new ArgumentException("Function contains29Feb can only be used for periods no longer than 1 year.");
            if (d1d == 29 && d1m == 2) return true;
            if (d2d == 29 && d2m == 2) return true;
            if (d1y == d2y)
            {
                if (d1m > 2 || d2m < 2 || (d2m == 2 && d2d < 29)) return false;
                return DateTime.IsLeapYear(d1y);
            }
            else
            {
                if (d2m < 2 || (d2m == 2 && d2d < 29)) return false;
                return DateTime.IsLeapYear(d2y);
            }
        }

        public static double GetDayCount(DateTime date1, DateTime date2, DayCountConvention dcc)
        {
            var sgn = date1 < date2 ? 1 : -1;
            var d1d = date1 < date2 ? date1.Day : date2.Day;
            var d2d = date1 < date2 ? date2.Day : date1.Day;
            var d1m = date1 < date2 ? date1.Month : date2.Month;
            var d2m = date1 < date2 ? date2.Month : date1.Month;
            var d1y = date1 < date2 ? date1.Year : date2.Year;
            var d2y = date1 < date2 ? date2.Year : date1.Year;
            switch (dcc)
            {
                case DayCountConvention.US_30_360:
                    if (d1m == 2 && d1d == DateTime.DaysInMonth(d1y, d1m) && d2m == 2 && d2d == DateTime.DaysInMonth(d2y, d2m)) d2d = 30;
                    if (d1m == 2 && d1d == DateTime.DaysInMonth(d1y, d1m)) d1d = 30;
                    if (d2d > 30 && d1d >= 30) d2d = 30;
                    d1d = Math.Min(d1d, 30);
                    return sgn * ((d2y - d1y) * 360.0 + (d2m - d1m) * 30.0 + (d2d - d1d));

                case DayCountConvention.Actual_Actual_Excel:
                    return sgn * (new DateTime(d2y, d2m, d2d).Date - new DateTime(d1y, d1m, d1d).Date).TotalDays;

                case DayCountConvention.Actual_360:
                    return sgn * (new DateTime(d2y, d2m, d2d).Date - new DateTime(d1y, d1m, d1d).Date).TotalDays;

                case DayCountConvention.Actual_365:
                    return sgn * (new DateTime(d2y, d2m, d2d).Date - new DateTime(d1y, d1m, d1d).Date).TotalDays;

                case DayCountConvention.European_30_360:
                    d2d = Math.Min(d2d, 30);
                    d1d = Math.Min(d1d, 30);
                    return sgn * ((d2y - d1y) * 360.0 + (d2m - d1m) * 30.0 + (d2d - d1d));

            }
            return 0;
        }

        public static double GetYearLength(DateTime date1, DateTime date2, DayCountConvention dcc)
        {
            var d1d = date1 < date2 ? date1.Day : date2.Day;
            var d2d = date1 < date2 ? date2.Day : date1.Day;
            var d1m = date1 < date2 ? date1.Month : date2.Month;
            var d2m = date1 < date2 ? date2.Month : date1.Month;
            var d1y = date1 < date2 ? date1.Year : date2.Year;
            var d2y = date1 < date2 ? date2.Year : date1.Year;
            switch (dcc)
            {
                case DayCountConvention.US_30_360:
                    return 360.0;

                case DayCountConvention.Actual_Actual_Excel:
                    if (isShorterThanYear(date1, date2))
                    {
                        if (d1y == d2y && DateTime.IsLeapYear(d1y)) return 366.0;
                        else if (contains29Feb(date1, date2) || (d2m == 2 && d2d == 29)) return 366.0;
                        else return 365.0;
                    }
                    else
                    {
                        double yearsCount = d2y - d1y + 1;
                        double days = GetDayCount(new DateTime(d1y, 1, 1), new DateTime(d2y + 1, 1, 1), dcc);
                        return days / yearsCount;
                    }

                case DayCountConvention.Actual_360:
                    return 360.0;

                case DayCountConvention.Actual_365:
                    return 365.0;

                case DayCountConvention.European_30_360:
                    return 360.0;
            }
            return 0;
        }

        public static double YearFraction(DateTime date1, DateTime date2, DayCountConvention dcc)
        {
            return GetDayCount(date1, date2, dcc) / GetYearLength(date1, date2, dcc);
        }

        public static Dictionary<(DateTime, DateTime, int, DayCountConvention, EndOfMonthConvention), DateTime[]> CouponCache = new Dictionary<(DateTime, DateTime, int, DayCountConvention, EndOfMonthConvention), DateTime[]>();
        public static DateTime[] GetCoupons(DateTime date, DateTime maturity, int frequency, DayCountConvention dcc, EndOfMonthConvention eoc = EndOfMonthConvention.DontAlign)
        {
            if (CouponCache.ContainsKey((date, maturity, frequency, dcc, eoc)))
                return CouponCache[(date, maturity, frequency, dcc, eoc)];

            List<DateTime> list = new List<DateTime>();

            int monthStep = (int)(12 / (double)frequency);

            DateTime currentDate = maturity.Date;
            bool eom = maturity.Date == new DateTime(maturity.Year, maturity.Month, DateTime.DaysInMonth(maturity.Year, maturity.Month));

            while (currentDate > date)
            {
                list.Add(currentDate);
                currentDate = currentDate.AddMonths(-monthStep);
                if (eom && eoc == EndOfMonthConvention.Align) currentDate = new DateTime(currentDate.Year, currentDate.Month, DateTime.DaysInMonth(maturity.Year, maturity.Month));
            }

            list.Reverse();
            DateTime[] output = list.ToArray();

            CouponCache[(date, maturity, frequency, dcc, eoc)] = output;
            return output;
        }

        public static DateTime NextCoupon(DateTime date, DateTime maturity, int frequency, DayCountConvention dcc)
        {
            var coupons = GetCoupons(date, maturity, frequency, dcc);
            if (coupons.Length == 0) return new DateTime();
            return coupons[0];
        }

        public static DateTime PrevCoupon(DateTime date, DateTime maturity, int frequency, DayCountConvention dcc)
        {
            int monthStep = (int)(12 / (double)frequency);
            var nextCoupon = NextCoupon(date, maturity, frequency, dcc);
            return nextCoupon.AddMonths(-monthStep);
        }

        public static int DaysSincePrevCoupon(DateTime date, DateTime maturity, int frequency, DayCountConvention dcc)
        {
            return (int)GetDayCount(PrevCoupon(date, maturity, frequency, dcc), date, dcc);
        }

        public static int DaysToNextCoupon(DateTime date, DateTime maturity, int frequency, DayCountConvention dcc)
        {
            return (int)GetDayCount(date, NextCoupon(date, maturity, frequency, dcc), dcc);
        }

        public static int NumberOfRemainingCoupons(DateTime date, DateTime maturity, int frequency, DayCountConvention dcc)
        {
            return GetCoupons(date, maturity, frequency, dcc).Count();
        }

        public static double DaysInCouponPeriod(DateTime date, DateTime maturity, int frequency, DayCountConvention dcc)
        {
            switch (dcc)
            {
                case DayCountConvention.US_30_360: return 360.0 / frequency;
                case DayCountConvention.Actual_Actual_Excel: return GetDayCount(PrevCoupon(date, maturity, frequency, dcc), NextCoupon(date, maturity, frequency, dcc), dcc);
                case DayCountConvention.Actual_360: return 360.0 / frequency;
                case DayCountConvention.Actual_365: return 365.0 / frequency;
                case DayCountConvention.European_30_360: return 360.0 / frequency;
                default: return GetDayCount(PrevCoupon(date, maturity, frequency, dcc), NextCoupon(date, maturity, frequency, dcc), dcc);
            }
        }

        public static double GetTenor(DateTime date, DateTime maturity, int frequency, DayCountConvention dcc)
        {
            var days = DayCount.DaysToNextCoupon(date, maturity, frequency, dcc);
            var length = DayCount.DaysInCouponPeriod(date, maturity, frequency, dcc);
            var coupons = DayCount.NumberOfRemainingCoupons(date, maturity, frequency, dcc);

            return coupons - 1 + days / (double)length;
        }
    }
}
