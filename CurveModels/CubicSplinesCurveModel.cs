using Microsoft.SolverFoundation.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Financial
{
    internal class CubicSplinesCurveModel : CurveModel
    {
        List<CubicSplinesSector> sectors = new List<CubicSplinesSector>();

        protected override void Recalculate()
        {
            sectors = DivideIntoSectors(nodes).ToList();
            sectors.ForEach(x => x.RecalculateSector());
            base.Recalculate();
        }

        protected override double GetFromModel(double t)
        {
            var s = sectors.FirstOrDefault(x => t >= x.Floor && t <= x.Ceil);
            if (s == null)
            {
                foreach (var sec in sectors)
                {
                    if (t < sec.Floor) s = sec;
                }
                if (s == null) s = sectors.LastOrDefault();
            }
            if (s == null)
            {
                return 0;
            }
            return s.Get(t);
        }

        private IEnumerable<CubicSplinesSector> DivideIntoSectors(List<CurveModelNode> nodes)
        {
            var t = nodes.Select(x => x.Maturity).ToArray();
            if (t.Length < 6)
            {
                yield return new CubicSplinesSector(base.nodes, nodes.First().Maturity, nodes.Last().Maturity);
            }
            else if (t.Length < 8)
            {
                int div1 = (int)Math.Floor(t.Length / 2.0);
                yield return new CubicSplinesSector(base.nodes, nodes.First().Maturity, nodes[div1].Maturity);
                yield return new CubicSplinesSector(base.nodes, nodes[div1].Maturity, nodes.Last().Maturity);
            }
            else
            {
                int div1 = (int)Math.Floor(t.Length / 3.0);
                int div2 = (int)Math.Floor(t.Length * 2.0 / 3.0);
                yield return new CubicSplinesSector(base.nodes, nodes.First().Maturity, nodes[div1].Maturity);
                yield return new CubicSplinesSector(base.nodes, nodes[div1].Maturity, nodes[div2].Maturity);
                yield return new CubicSplinesSector(base.nodes, nodes[div2].Maturity, nodes.Last().Maturity);
            }
        }
    }

    class CubicSplinesSector
    {
        public double Floor { get; private set; }
        public double Ceil { get; private set; }
        IEnumerable<CurveModelNode> nodes;

        SolverContext solver;
        Model model;

        double r0;
        double a;
        double b;
        double c;

        Decision d_a = new Decision(Domain.Real, "a");
        Decision d_b = new Decision(Domain.Real, "b");
        Decision d_c = new Decision(Domain.Real, "c");

        internal CubicSplinesSector(IEnumerable<CurveModelNode> nodes, double floor, double ceil)
        {
            // Finding parameters by Solver
            solver = SolverContext.GetContext();
            model = solver.CreateModel();

            this.Floor = floor;
            this.Ceil = ceil;
            this.nodes = nodes;

            r0 = nodes.FirstOrDefault()?.Value ?? 0;
        }

        protected internal void RecalculateSector()
        {
            solver.ClearModel();
            model = solver.CreateModel();

            d_a = new Decision(Domain.Real, "a");
            d_b = new Decision(Domain.Real, "b");
            d_c = new Decision(Domain.Real, "c");
            d_a.SetInitialValue(0, new object[] { });
            d_b.SetInitialValue(0, new object[] { });
            d_c.SetInitialValue(0, new object[] { });

            model.AddDecisions(d_a, d_b, d_c);
            model.AddGoal("Goal", GoalKind.Minimize, Calculate(d_a, d_b, d_c, r0));

            var solution = solver.Solve();

            a = d_a.GetDouble();
            b = d_b.GetDouble();
            c = d_c.GetDouble();
        }

        Term Calculate(Decision a, Decision b, Decision c, double r0)
        {
            Term approx;
            Term diff = 0;

            foreach (var v in nodes)
            {
                approx = r0 + a * v.Maturity + b * Model.Power(v.Maturity, 2) + c * Model.Power(v.Maturity, 3);
                diff += Model.Power((approx - v.Value) * v.Score, 2);
            }

            return diff;
        }

        internal double Get(double t)
        {
            if (t <= 0) t = 1 / 365;
            return r0 + a * t + b * Math.Pow(t, 2) + c * Math.Pow(t, 3);
        }
    }
}
