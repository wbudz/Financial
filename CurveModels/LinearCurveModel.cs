using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Financial
{
    internal class LinearCurveModel : CurveModel
    {
        internal LinearCurveModel() : base()
        {
        }

        protected override double GetFromModel(double t)
        {
            if (t <= nodes.First().Maturity) return nodes.First().Value;
            if (t >= nodes.Last().Maturity) return nodes.Last().Value;

            var n1 = nodes.Last(x => x.Maturity <= t);
            var n2 = nodes.First(x => x.Maturity >= t);

            return n1.Value + ((t - n1.Maturity) / (n2.Maturity - n1.Maturity)) * (n2.Value - n1.Value);
        }
    }
}
