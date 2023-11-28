using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Financial
{
    /// <summary>
    /// Represents single node on a curve, i.e. maturity for which value (yield) is known.
    /// </summary>
    public class CurveModelNode
    {
        /// <summary>
        /// Maturity (in years)
        /// </summary>
        public double Maturity { get; private set; }

        /// <summary>
        /// Value (yield)
        /// </summary>
        public double Value { get; private set; }

        /// <summary>
        /// Optional score which indicates how strongly the model should take this node into account when plotting the curve. Nodes originating from inputs of low liquidity, outliers or of otherwise dubious quality should be assigned low score. Non-negative values should be used for scores where 0 means that the node would be ignored and 1 should denote 'standard' weight.
        /// </summary>
        public double Score { get; private set; }

        /// <summary>
        /// Label which indicates node's maturity, value and score, if applicable.
        /// </summary>
        public string Label
        {
            get
            {
                if (Score == 1)
                    return String.Format("X {0:0.##} Y {1:0.##}", Maturity, Value);
                else
                    return String.Format("X {0:0.##} Y {1:0.##} (S {2:0.##})", Maturity, Value, Score);
            }
        }

        public CurveModelNode(double maturity, double value, double score = 1)
        {
            Maturity = maturity;
            Value = value;
            Score = Math.Max(0, score);
        }
    }
}
