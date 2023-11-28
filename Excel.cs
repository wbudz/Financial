using System;

namespace Financial
{
    public class Excel
    {
        public static double COUPDAYSBS(DateTime Settlement, DateTime Maturity, int Frequency, int Basis)
        {
            return DayCount.DaysSincePrevCoupon(Settlement, Maturity, Frequency, BasisToDCC(Basis));
        }

        static DayCountConvention BasisToDCC(int basis)
        {
            switch (basis)
            {
                case 0: return DayCountConvention.US_30_360;
                case 1: return DayCountConvention.Actual_Actual_Excel;
                case 2: return DayCountConvention.Actual_360;
                case 3: return DayCountConvention.Actual_365;
                case 4: return DayCountConvention.European_30_360;
                default: return DayCountConvention.US_30_360;
            }
        }
    }
}
