using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Financial
{
    public class Solver
    {
        public enum GoalSeekMethod { Secant, Bisect }

        static internal double NewtonSolver(Func<double, double> function, Func<double, double> derivativeFunction, int iterations, double startingValue = 0d)
        {
            double x = startingValue;
            for (int i = 0; i < iterations; i++)
            {
                x -= function(x) / derivativeFunction(x);
            }
            return x;
        }

        static internal double NewtonSolver(Func<double, double> function, int iterations, double tolerance, double startingValue = 0d)
        {
            return NewtonSolver(function, x => ((function(x + tolerance) - function(x - tolerance)) / tolerance * 2), iterations, startingValue);
        }

        static internal double PriceDerivative(DateTime date, DateTime maturity, double couponRate, double yield, double redemption, int frequency, DayCountConvention dcc)
        {
            double E = DayCount.DaysInCouponPeriod(date, maturity, frequency, dcc);
            double N = DayCount.NumberOfRemainingCoupons(date, maturity, frequency, dcc);
            double A = DayCount.DaysSincePrevCoupon(date, maturity, frequency, dcc);
            double DSC = DayCount.DaysToNextCoupon(date, maturity, frequency, dcc);

            double presentValueOfRedemption = 100 * (1 - N - DSC / E) * Math.Pow(1 + yield, -N - DSC / E);
            double presentValueOfCouponPayments = 0;
            for (int k = 1; k <= N; k++)
            {
                presentValueOfCouponPayments += (100 * (1 - k - DSC / E) * couponRate / frequency * Math.Pow(1 + yield, -k - DSC / E)) / Math.Pow(1, 2);
            }
            double accruedInterest = 100 * (couponRate / frequency) * (A / E);
            return presentValueOfRedemption + presentValueOfCouponPayments - accruedInterest;
        }

        public static double GoalSeek(Func<double, double> f, double goal, double tolerance = 0.001, int iterations = 100)
        {
            return Secant(f, goal, 0, 0.0001, tolerance, iterations, out int iterationsUsed, out double errorEstimate);
        }

        public static double GoalSeek(GoalSeekMethod method, Func<double, double> f, double goal, double left = -1, double right = 1, double tolerance = 0.001, int iterations = 100)
        {
            int iterationsUsed;
            double errorEstimate;
            switch (method)
            {
                case GoalSeekMethod.Secant: return Secant(f, goal, left, right, tolerance, iterations, out iterationsUsed, out errorEstimate);
                case GoalSeekMethod.Bisect: return Bisect(f, goal, left, right, tolerance, iterations, out iterationsUsed, out errorEstimate);
                default: return double.NaN;
            }
        }

        static double Secant(Func<double, double> f, double goal, double left, double right, double tolerance, int iterations, out int iterationsUsed, out double errorEstimate)
        {
            if (tolerance <= 0.0)
                throw new ArgumentOutOfRangeException("Tolerance must be positive.");
            if (iterations <= 0)
                throw new ArgumentOutOfRangeException("Number of iterations must be positive.");

            Func<double, double> g = delegate (double x) { return f(x) - goal; };

            double p2, p1, p0;
            int i;
            p0 = g(left);
            p1 = g(right);
            p2 = p1 - g(p1) * (p1 - p0) / (g(p1) - g(p0));

            for (i = 0; System.Math.Abs(p2 - p1) > tolerance && i < iterations; i++)
            {
                p0 = p1;
                p1 = p2;
                p2 = p1 - g(p1) * (p1 - p0) / (g(p1) - g(p0));
            }
            if (i < iterations)
            {
                iterationsUsed = i;
                errorEstimate = System.Math.Abs(p2 - p1);
                return p2;
            }
            else
            {
                iterationsUsed = i;
                errorEstimate = System.Math.Abs(p2 - p1);
                return double.NaN;
            }
        }

        static double Bisect(Func<double, double> f, double goal, double left, double right, double tolerance, int iterations, out int iterationsUsed, out double errorEstimate)
        {
            if (tolerance <= 0.0)
                throw new ArgumentOutOfRangeException("Tolerance must be positive.");
            if (iterations <= 0)
                throw new ArgumentOutOfRangeException("Number of iterations must be positive.");

            iterationsUsed = 0;
            errorEstimate = double.MaxValue;

            // Standardize the problem.  To solve f(x) = target,
            // solve g(x) = 0 where g(x) = f(x) - target.
            Func<double, double> g = delegate (double x) { return f(x) - goal; };

            double g_left = g(left);  // evaluation of f at left end of interval
            double g_right = g(right);
            double mid;
            double g_mid;
            if (g_left * g_right >= 0.0)
            {
                string str = "Invalid starting bracket. Function must be above target on one end and below target on other end.";
                string msg = string.Format("{0} Target: {1}. f(left) = {2}. f(right) = {3}", str, g_left + goal, g_right + goal);
                throw new ArgumentException(msg);
            }

            double intervalWidth = right - left;

            for
            (
                iterationsUsed = 0;
                iterationsUsed < iterations && intervalWidth > tolerance;
                iterationsUsed++
            )
            {
                intervalWidth *= 0.5;
                mid = left + intervalWidth;

                if ((g_mid = g(mid)) == 0.0)
                {
                    errorEstimate = 0.0;
                    return mid;
                }
                if (g_left * g_mid < 0.0)           // g changes sign in (left, mid)    
                    g_right = g(right = mid);
                else                            // g changes sign in (mid, right)
                    g_left = g(left = mid);
            }
            errorEstimate = right - left;
            return left;
        }

        static double Brent(Func<double, double> f, double left, double right, double tolerance, double target, int iterations, out int iterationsUsed, out double errorEstimate)
        {
            if (tolerance <= 0.0)
                throw new ArgumentOutOfRangeException("Tolerance must be positive.");
            if (iterations <= 0)
                throw new ArgumentOutOfRangeException("Number of iterations must be positive.");

            errorEstimate = double.MaxValue;

            // Standardize the problem.  To solve g(x) = target,
            // solve f(x) = 0 where f(x) = g(x) - target.
            Func<double, double> g = delegate (double x) { return f(x) - target; };

            // Implementation and notation based on Chapter 4 in
            // "Algorithms for Minimization without Derivatives"
            // by Richard Brent.

            double c, d, e, fa, fb, fc, tol, m, p, q, r, s;

            // set up aliases to match Brent's notation
            double a = left; double b = right; double t = tolerance;
            iterationsUsed = 0;

            fa = g(a);
            fb = g(b);

            if (fa * fb > 0.0)
            {
                string str = "Invalid starting bracket. Function must be above target on one end and below target on other end.";
                string msg = string.Format("{0} Target: {1}. f(left) = {2}. f(right) = {3}", str, target, fa + target, fb + target);
                throw new ArgumentException(msg);
            }

label_int:
            c = a; fc = fa; d = e = b - a;
label_ext:
            if (Math.Abs(fc) < Math.Abs(fb))
            {
                a = b; b = c; c = a;
                fa = fb; fb = fc; fc = fa;
            }

            iterationsUsed++;

            tol = 2.0 * t * Math.Abs(b) + t;
            errorEstimate = m = 0.5 * (c - b);
            if (Math.Abs(m) > tol && fb != 0.0) // exact comparison with 0 is OK here
            {
                // See if bisection is forced
                if (Math.Abs(e) < tol || Math.Abs(fa) <= Math.Abs(fb))
                {
                    d = e = m;
                }
                else
                {
                    s = fb / fa;
                    if (a == c)
                    {
                        // linear interpolation
                        p = 2.0 * m * s; q = 1.0 - s;
                    }
                    else
                    {
                        // Inverse quadratic interpolation
                        q = fa / fc; r = fb / fc;
                        p = s * (2.0 * m * q * (q - r) - (b - a) * (r - 1.0));
                        q = (q - 1.0) * (r - 1.0) * (s - 1.0);
                    }
                    if (p > 0.0)
                        q = -q;
                    else
                        p = -p;
                    s = e; e = d;
                    if (2.0 * p < 3.0 * m * q - Math.Abs(tol * q) && p < Math.Abs(0.5 * s * q))
                        d = p / q;
                    else
                        d = e = m;
                }
                a = b; fa = fb;
                if (Math.Abs(d) > tol)
                    b += d;
                else if (m > 0.0)
                    b += tol;
                else
                    b -= tol;
                if (iterationsUsed == iterations)
                    return b;

                fb = g(b);
                if ((fb > 0.0 && fc > 0.0) || (fb <= 0.0 && fc <= 0.0))
                    goto label_int;
                else
                    goto label_ext;
            }
            else
                return b;
        }

        static double Newton(Func<double, double> f, Func<double, double> fprime, double guess, double tolerance, double target, int iterations, out int iterationsUsed, out double errorEstimate)
        {
            if (tolerance <= 0.0)
                throw new ArgumentOutOfRangeException("Tolerance must be positive.");
            if (iterations <= 0)
                throw new ArgumentOutOfRangeException("Number of iterations must be positive.");

            iterationsUsed = 0;
            errorEstimate = double.MaxValue;

            // Standardize the problem.  To solve f(x) = target,
            // solve g(x) = 0 where g(x) = f(x) - target.
            // Note that f(x) and g(x) have the same derivative.
            Func<double, double> g = delegate (double x) { return f(x) - target; };

            double oldX, newX = guess;
            for
            (
                iterationsUsed = 0;
                iterationsUsed < iterations && errorEstimate > tolerance;
                iterationsUsed++
            )
            {
                oldX = newX;
                double gx = g(oldX);
                double gprimex = fprime(oldX);
                double absgprimex = Math.Abs(gprimex);
                if (absgprimex > 1.0 || Math.Abs(gx) < double.MaxValue * absgprimex)
                {
                    // The division will not overflow
                    newX = oldX - gx / gprimex;
                    errorEstimate = Math.Abs(newX - oldX);
                }
                else
                {
                    newX = oldX;
                    errorEstimate = double.MaxValue;
                    break;
                }
            }
            return newX;
        }

    }
}
