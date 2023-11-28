using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Financial
{
    /// <summary>
    /// Defines method of interpolation / extrapolation when getting points on the curve for maturities not equal to those defined by the nodes.
    /// Linear - linear interpolation; for values beyond first and last maturity, values for first and last maturity respectively are used.
    /// CubicSplines - https://en.wikipedia.org/wiki/Spline_interpolation
    /// NelsonSiegelSvensson - https://www.youtube.com/watch?v=uQnA9j_FvAg
    /// </summary>
    public enum CurveModelType { Linear, NelsonSiegelSvensson, CubicSplines }

    public class Curve
    {
        CurveModel model;

        public string Name { get; set; }

        public Curve(CurveModelType type, string name)
        {
            Name = name;
            switch (type)
            {
                case CurveModelType.Linear:
                    model = new LinearCurveModel();
                    break;
                case CurveModelType.NelsonSiegelSvensson:
                    model = new NelsonSiegelSvenssonCurveModel();
                    break;
                case CurveModelType.CubicSplines:
                    model = new CubicSplinesCurveModel();
                    break;
                default:
                    break;
            }
            model = new LinearCurveModel();
        }

        private Curve(CurveModel model, string name, IEnumerable<Point> nodes)
        {
            Name = name;
            this.model = model;
            this.model.ClearNodes();
            this.model.AddNodes(nodes);
        }

        public IEnumerable<Point> Get(double min, double max, double step)
        {
            return model.Get(min, max, step);
        }

        public double Get(double maturity)
        {
            return model.Get(maturity);
        }

        public IEnumerable<Point> GetNodes()
        {
            return model.GetNodes();
        }

        public void AddNode(double maturity, double value)
        {
            model.AddNode(maturity, value);
        }

        public void AddNodes(IEnumerable<Point> nodes)
        {
            model.AddNodes(nodes);
        }

        public void ClearNodes()
        {
            model.ClearNodes();
        }

        public decimal CalculatePresentValue(double tenor, decimal amount)
        {
            return amount * (decimal)(1 / Math.Pow(1 + Get(tenor), tenor));
        }

        public double CalculatePresentValue(double tenor, double amount)
        {
            return amount * (1 / Math.Pow(1 + Get(tenor), tenor));
        }

        public static Curve ConstructForwardCurve(Curve source, double maturityShift)
        {
            var nodes = source.GetNodes().ToArray();
            if (nodes.Length < 2) return source;

            var forward = new Point[nodes.Length];
            var value = source.Get(maturityShift);

            for (int i = 0; i < nodes.Length; i++)
            {
                forward[i] = new Point(nodes[i].X,
                    Math.Pow((1 / Math.Pow(1 + value, maturityShift)) / (1 / Math.Pow(1 + source.Get(nodes[i].X + maturityShift), nodes[i].X + maturityShift)), (1 / maturityShift)) - 1
                    );
            }

            return new Curve(source.model, source.Name, forward);
        }
    }
}
