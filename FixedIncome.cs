using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Financial
{
    public static class FixedIncome
    {
        public static double Interest(DateTime date, DateTime maturity, double couponRate, int frequency, DayCountConvention dcc, bool rounded = false)
        {
            if (date >= maturity) throw new Exception("Cannot calculate interest on the maturity day or a later date.");
            double coupDays = DayCount.DaysInCouponPeriod(date, maturity, frequency, dcc);
            double coupSince = DayCount.DaysSincePrevCoupon(date, maturity, frequency, dcc);
            double interest = coupSince / coupDays * couponRate / frequency * 100;
            return rounded ? Math.Round(interest, 3, MidpointRounding.AwayFromZero) : interest;
        }

        public static double Price(DateTime date, DateTime maturity, double couponRate, double yield, double redemption, int frequency, DayCountConvention dcc)
        {
            double dirtyPrice = DirtyPrice(date, maturity, couponRate, yield, redemption, frequency, dcc);
            double interest = Interest(date, maturity, couponRate, frequency, dcc);
            return dirtyPrice - interest;
        }

        public static double Price(DateTime date, DateTime maturity, double couponRate, Curve curve, double redemption, int frequency, DayCountConvention dcc)
        {
            double dirtyPrice = DirtyPrice(date, maturity, couponRate, curve, redemption, frequency, dcc);
            double interest = Interest(date, maturity, couponRate, frequency, dcc);
            return dirtyPrice - interest;
        }

        static double Price(Cashflows cf, DateTime date, DateTime maturity, double couponRate, double yield, double redemption, int frequency, DayCountConvention dcc)
        {
            double dirtyPrice = DirtyPrice(cf, date, maturity, couponRate, yield, redemption, frequency, dcc);
            double interest = Interest(date, maturity, couponRate, frequency, dcc, false);
            return dirtyPrice - interest;
        }

        private static double CalculateDirtyPriceForSinglePeriod(DateTime date, DateTime maturity, double couponRate, double yield, double redemption, int frequency, DayCountConvention dcc)
        {
            var A = DayCount.DaysSincePrevCoupon(date, maturity, frequency, dcc);
            var E = DayCount.DaysInCouponPeriod(date, maturity, frequency, dcc);
            var T1 = 100 * couponRate / frequency + redemption;
            var T2 = yield / frequency * (E - A) / E + 1;
            return T1 / T2;
        }

        public static double DirtyPrice(DateTime date, DateTime maturity, double couponRate, double yield, double redemption, int frequency, DayCountConvention dcc)
        {
            var couponCount = DayCount.NumberOfRemainingCoupons(date, maturity, frequency, dcc);
            // Calculate for one coupon period
            if (couponCount <= 1)
            {
                return CalculateDirtyPriceForSinglePeriod(date, maturity, couponRate, yield, redemption, frequency, dcc);
            }
            // Calculate for multiple periods
            else
            {
                var cashflows = new Cashflows(date, maturity, couponRate, yield, redemption, frequency, dcc);
                return cashflows.GetPresentValue();
            }
        }

        public static double DirtyPrice(DateTime date, DateTime maturity, double couponRate, Curve curve, double redemption, int frequency, DayCountConvention dcc)
        {
            // Calculate for a single period
            if (DayCount.NextCoupon(date, maturity, frequency, dcc) >= maturity)
            {
                // TODO: insert proper yield
                return CalculateDirtyPriceForSinglePeriod(date, maturity, couponRate, couponRate, redemption, frequency, dcc);
            }
            // Calculate for multiple periods
            else
            {
                var cashflows = new Cashflows(date, maturity, couponRate, curve, redemption, frequency, dcc);
                return cashflows.GetPresentValue();
            }
        }

        static double DirtyPrice(Cashflows cf, DateTime date, DateTime maturity, double couponRate, double yield, double redemption, int frequency, DayCountConvention dcc)
        {
            cf.Yield = yield;
            return cf.GetPresentValue();
        }

        public static double Duration(DateTime date, DateTime maturity, double couponRate, double yield, double redemption, int frequency, DayCountConvention dcc)
        {
            var cashflows = new Cashflows(date, maturity, couponRate, yield, redemption, frequency, dcc);
            return cashflows.GetDuration();
        }

        public static double MDuration(DateTime date, DateTime maturity, double couponRate, double yield, double redemption, int frequency, DayCountConvention dcc)
        {
            return Duration(date, maturity, couponRate, yield, redemption, frequency, dcc) / (1 + yield / frequency);
        }

        public static double Yield(DateTime date, DateTime maturity, double couponRate, double price, double redemption, int frequency, DayCountConvention dcc)
        {
            // Last coupon period
            if (DayCount.NextCoupon(date, maturity, frequency, dcc) >= maturity)
            {
                double length = DayCount.DaysInCouponPeriod(date, maturity, frequency, dcc);
                double days = DayCount.DaysSincePrevCoupon(date, maturity, frequency, dcc);

                return ((1 + couponRate / frequency) - (price / 100 + days / length * couponRate / frequency)) / (price / 100 + days / length * couponRate / frequency) * length * frequency / (length - days);
            }

            // Create resident cashflow structure
            var cashflows = new Cashflows(date, maturity, couponRate, 0, redemption, frequency, dcc);

            return Solver.NewtonSolver(
                x => Price(cashflows, date, maturity, couponRate, x, redemption, frequency, dcc) - price,
                y => Solver.PriceDerivative(date, maturity, couponRate, y, redemption, frequency, dcc), 100);
        }
    }
}
