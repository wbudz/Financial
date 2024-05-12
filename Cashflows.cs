using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Financial
{
    public class Cashflows
    {
        List<Cashflow> cf = new List<Cashflow>();
        public int Count { get { return cf.Count; } }
        public double TimeToMaturity { get { return cf.Last().Tenor; } }

        int frequency;
        double yield;
        public double Yield
        {
            get
            {
                return yield;
            }
            set
            {
                yield = value;
                for (int i = 0; i < Count; i++)
                {
                    cf[i].PresentValue = cf[i].Amount / Math.Pow(1 + yield / frequency, cf[i].TimeCoefficient);
                }
            }
        }

        private double purchaseDaysSincePrevCoupon;

        private double purchaseDaysInCouponPeriod;

        public Cashflows(DateTime date, DateTime maturity, double couponRate, double yield, double redemption, int frequency, DayCountConvention dcc, decimal nominal)
        {
            var dates = DayCount.GetCoupons(date, maturity, frequency, dcc);

            var days = DayCount.DaysToNextCoupon(date, maturity, frequency, dcc);
            var length = DayCount.DaysInCouponPeriod(date, maturity, frequency, dcc);
            var coupons = DayCount.NumberOfRemainingCoupons(date, maturity, frequency, dcc);

            purchaseDaysSincePrevCoupon = DayCount.DaysSincePrevCoupon(date, maturity, frequency, dcc);
            purchaseDaysInCouponPeriod = DayCount.DaysInCouponPeriod(date, maturity, frequency, dcc);

            this.frequency = frequency;
            this.yield = yield;

            if (dates.Count() == 0) return;
            // coupons
            for (int i = 0; i < dates.Count(); i++)
            {
                cf.Add(new Cashflow((double)nominal * couponRate / frequency, dates[i], yield, frequency, i + days / (double)length, DayCount.YearFraction(date, dates.ElementAt(i), dcc)));
            }
            // redemption
            cf.Add(new Cashflow((double)nominal * redemption / 100.0, maturity, yield, frequency, coupons - 1 + days / (double)length, DayCount.YearFraction(date, maturity, dcc)));
        }

        public Cashflows(DateTime date, DateTime maturity, double couponRate, Curve curve, double redemption, int frequency, DayCountConvention dcc, decimal nominal)
        {
            var dates = DayCount.GetCoupons(date, maturity, frequency, dcc);

            var days = DayCount.DaysToNextCoupon(date, maturity, frequency, dcc);
            var length = DayCount.DaysInCouponPeriod(date, maturity, frequency, dcc);
            var coupons = DayCount.NumberOfRemainingCoupons(date, maturity, frequency, dcc);

            purchaseDaysSincePrevCoupon = DayCount.DaysSincePrevCoupon(date, maturity, frequency, dcc);
            purchaseDaysInCouponPeriod = DayCount.DaysInCouponPeriod(date, maturity, frequency, dcc);

            this.frequency = frequency;
            SetYields(curve);

            if (dates.Count() == 0) return;
            // coupons
            for (int i = 0; i < dates.Count(); i++)
            {
                cf.Add(new Cashflow((double)nominal * couponRate / frequency, dates.ElementAt(i), yield, frequency, i + days / (double)length, DayCount.YearFraction(date, dates.ElementAt(i), dcc)));
            }
            // redemption
            cf.Add(new Cashflow((double)nominal * redemption / 100.0, maturity, yield, frequency, coupons - 1 + days / (double)length, DayCount.YearFraction(date, maturity, dcc)));
        }

        public Cashflows(DateTime date, DateTime maturity, double couponRate, double yield, double redemption, int frequency, DayCountConvention dcc) :
            this(date, maturity, couponRate, yield, redemption, frequency, dcc, 100m)
        {
        }

        public Cashflows(DateTime date, DateTime maturity, double couponRate, Curve curve, double redemption, int frequency, DayCountConvention dcc) :
            this(date, maturity, couponRate, curve, redemption, frequency, dcc, 100m)
        {
        }

        public Cashflows(Cashflows other)
        {
            this.cf = new List<Cashflow>();
            foreach (var item in other.cf)
            {
                this.cf.Add(new Cashflow(item));
            }
            this.frequency = other.frequency;
            this.yield = other.yield;
        }

        public double GetPresentValue()
        {
            return cf.Sum(x => x.PresentValue);
        }

        public double GetDuration()
        {
            double outputN = 0;
            for (int i = 0; i < Count; i++)
            {
                outputN += cf[i].PresentValue * cf[i].TimeCoefficient;
            }
            double outputD = GetPresentValue();
            return outputN / outputD;
        }

        public DateTime GetDate(int coupon)
        {
            return cf[coupon].Date;
        }

        public double GetFutureValueOfAFlow(int coupon)
        {
            return cf[coupon].Amount;
        }

        public double GetPresentValueOfAFlow(int coupon)
        {
            return cf[coupon].PresentValue;
        }

        public double GetTenor(int coupon)
        {
            return cf[coupon].Tenor;
        }

        public void SetYields(IEnumerable<double> yields)
        {
            if (yields.Count() != Count) throw new ArgumentException("Invalid amount of yields provided.");
            for (int i = 0; i < Count; i++)
            {
                cf[i].PresentValue = cf[i].Amount / Math.Pow(1 + yields.ElementAt(i) / frequency, cf[i].TimeCoefficient);
            }

            if (Count <= 2)
            {
                var pr = GetPresentValue();
                yield = ((1 + cf[0].Amount) - (pr / 100 + purchaseDaysSincePrevCoupon / purchaseDaysInCouponPeriod * cf[0].Amount)) / 
                    (pr / 100 + purchaseDaysSincePrevCoupon / purchaseDaysInCouponPeriod * cf[0].Amount) *
                    purchaseDaysInCouponPeriod / (purchaseDaysInCouponPeriod - purchaseDaysSincePrevCoupon);
            }
            else
            {
                yield = Solver.GoalSeek(Solver.GoalSeekMethod.Bisect, delegate (double x)
                {
                    double[] pv = new double[Count];
                    for (int i = 0; i < Count; i++)
                    {
                        pv[i] = cf[i].Amount / Math.Pow(1 + x / frequency, cf[i].TimeCoefficient);
                    }
                    return pv.Sum();
                },
                GetPresentValue(), -0.90, 0.90, tolerance: double.Epsilon, iterations: 1000);
            }
        }

        public void SetYields(Curve curve)
        {
            for (int i = 0; i < Count; i++)
            {
                cf[i].PresentValue = cf[i].Amount / Math.Pow(1 + curve.Get(cf[i].Tenor) / frequency, cf[i].TimeCoefficient);
            }

            if (Count <= 2)
            {
                var pr = GetPresentValue();
                yield = ((1 + cf[0].Amount) - (pr / 100 + purchaseDaysSincePrevCoupon / purchaseDaysInCouponPeriod * cf[0].Amount)) / 
                    (pr / 100 + purchaseDaysSincePrevCoupon / purchaseDaysInCouponPeriod * cf[0].Amount) *
                    purchaseDaysInCouponPeriod / (purchaseDaysInCouponPeriod - purchaseDaysSincePrevCoupon);
            }
            else
            {
                yield = Solver.GoalSeek(Solver.GoalSeekMethod.Bisect, delegate (double x)
                {
                    double[] pv = new double[Count];
                    for (int i = 0; i < Count; i++)
                    {
                        pv[i] = cf[i].Amount / Math.Pow(1 + x / frequency, cf[i].TimeCoefficient);
                    }
                    return pv.Sum();
                },
                GetPresentValue(), -0.90, 0.90, tolerance: double.Epsilon, iterations: 1000);
            }
        }

        public double GetSpreadOverCurve(IEnumerable<KeyValuePair<double, double>> rfr, bool highPrecision)
        {
            var goal = GetPresentValue();
            if (Count <= 2)
            {
                var rf = Interpolation.Linear(cf[0].Tenor, rfr);
                return ((1 + cf[0].Amount) - (goal / 100 + purchaseDaysSincePrevCoupon / purchaseDaysInCouponPeriod * cf[0].Amount)) / 
                    (goal / 100 + purchaseDaysSincePrevCoupon / purchaseDaysInCouponPeriod * cf[0].Amount) * 
                    purchaseDaysInCouponPeriod / (purchaseDaysInCouponPeriod - purchaseDaysSincePrevCoupon) - rf;
            }
            else
            {
                return Solver.GoalSeek(Solver.GoalSeekMethod.Bisect, delegate (double x)
                {
                    double[] pv = new double[Count];
                    for (int i = 0; i < Count; i++)
                    {
                        var rf = Interpolation.Linear(cf[i].Tenor, rfr);
                        pv[i] = cf[i].Amount / Math.Pow(1 + (rf + x) / frequency, cf[i].TimeCoefficient);
                    }
                    return pv.Sum();
                }, GetPresentValue(), -0.90, 0.90, tolerance: highPrecision ? double.Epsilon : 0.00001, iterations: highPrecision ? 1000 : 100);
            }
        }
    }

    public class Cashflow
    {
        public double Amount { get; set; }
        public DateTime Date { get; set; }
        public double TimeCoefficient { get; set; }
        public double Tenor { get; set; }
        public double PresentValue { get; set; }

        public Cashflow(double amount, DateTime date, double yield, int frequency, double timeCoefficient, double tenor)
        {
            this.Amount = amount;
            this.Date = date;
            this.TimeCoefficient = timeCoefficient;
            this.Tenor = tenor;
            this.PresentValue = amount / Math.Pow(1 + yield / frequency, timeCoefficient);
        }

        public Cashflow(Cashflow other)
        {
            this.Amount = other.Amount;
            this.Date = other.Date;
            this.TimeCoefficient = other.TimeCoefficient;
            this.Tenor = other.Tenor;
            this.PresentValue = other.PresentValue;
        }

        public override string ToString()
        {
            return String.Format("{0}: FV:{1:N5} / PV:{2:N5}", Date.ToShortDateString(), Amount, PresentValue);
        }
    }
}
