using Microsoft.SolverFoundation.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Financial
{
    internal class NelsonSiegelSvenssonCurveModel : CurveModel
    {
        double beta1 = 0.1;
        double beta2 = 0.1;
        double beta3 = 0.1;
        double beta4 = 0.1;
        double lambda1 = 1.0;
        double lambda2 = 1.0;

        Decision d_beta1 = new Decision(Domain.Real, "beta1");
        Decision d_beta2 = new Decision(Domain.Real, "beta2");
        Decision d_beta3 = new Decision(Domain.Real, "beta3");
        Decision d_beta4 = new Decision(Domain.Real, "beta4");
        Decision d_lambda1 = new Decision(Domain.Real, "lambda1");
        Decision d_lambda2 = new Decision(Domain.Real, "lambda2");

        SolverContext solver;
        Model model;

        internal NelsonSiegelSvenssonCurveModel() : base()
        {
            // Finding parameters by Solver
            solver = SolverContext.GetContext();
            model = solver.CreateModel();
        }

        protected override void Recalculate()
        {
            solver.ClearModel();
            model = solver.CreateModel();

            d_beta1 = new Decision(Domain.Real, "beta1");
            d_beta2 = new Decision(Domain.Real, "beta2");
            d_beta3 = new Decision(Domain.Real, "beta3");
            d_beta4 = new Decision(Domain.Real, "beta4");
            d_lambda1 = new Decision(Domain.Real, "lambda1");
            d_lambda2 = new Decision(Domain.Real, "lambda2");
            d_beta1.SetInitialValue(beta1, new object[] { });
            d_beta2.SetInitialValue(beta2, new object[] { });
            d_beta3.SetInitialValue(beta3, new object[] { });
            d_beta4.SetInitialValue(beta4, new object[] { });
            d_lambda1.SetInitialValue(lambda1, new object[] { });
            d_lambda2.SetInitialValue(lambda2, new object[] { });

            model.AddDecisions(d_beta1, d_beta2, d_beta3, d_beta4, d_lambda1, d_lambda2);
            model.AddGoal("Goal", GoalKind.Minimize, Calculate(d_beta1, d_beta2, d_beta3, d_beta4, d_lambda1, d_lambda2));
            var solution = solver.Solve();

            beta1 = d_beta1.GetDouble();
            beta2 = d_beta2.GetDouble();
            beta3 = d_beta3.GetDouble();
            beta4 = d_beta4.GetDouble();
            lambda1 = d_lambda1.GetDouble();
            lambda2 = d_lambda2.GetDouble();

            base.Recalculate();
        }

        Term Calculate(Decision beta1, Decision beta2, Decision beta3, Decision beta4, Decision lambda1, Decision lambda2)
        {
            Term approx;
            Term diff = 0;

            foreach (var v in nodes)
            {
                approx = beta1 +
                    beta2 * (1 - Model.Exp(-v.Maturity / lambda1)) / (v.Maturity / lambda1) +
                    beta3 * ((1 - Model.Exp(-v.Maturity / lambda1)) / (v.Maturity / lambda1) - Model.Exp(-v.Maturity / lambda1)) +
                    beta4 * (((1 - Model.Exp(-v.Maturity / lambda2)) / (v.Maturity / lambda2) - Model.Exp(-v.Maturity / lambda2)));
                diff = diff + Model.Power((approx - v.Value) * v.Score, 2);
            }

            return diff;
        }

        static Term Calculate(Decision beta1, Decision beta2)
        {
            return Model.Power(beta1 + 2, 2) + beta1 + 5;
        }

        protected override double GetFromModel(double t)
        {
            if (t <= 0) t = 1.0 / 365.0;
            return beta1 +
                    beta2 * (1 - Math.Exp(-t / lambda1)) / (t / lambda1) +
                    beta3 * ((1 - Math.Exp(-t / lambda1)) / (t / lambda1) - Math.Exp(-t / lambda1)) +
                    beta4 * (((1 - Math.Exp(-t / lambda2)) / (t / lambda2) - Math.Exp(-t / lambda2)));
        }
    }
}
