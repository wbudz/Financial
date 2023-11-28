using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Financial
{
    /// <summary>
    /// Represents a curve model which allows for interpolation and extrapolation beyond defined curve nodes.
    /// </summary>
    internal abstract class CurveModel
    {
        protected List<CurveModelNode> nodes { get; set; } = new List<CurveModelNode>();
        private Dictionary<double, double> cache { get; set; } = new Dictionary<double, double>();

        protected bool recalculationNecessary = false;

        protected virtual void Recalculate()
        {
            cache.Clear();
            recalculationNecessary = false;
        }

        private void AddInOrder(double maturity, double value)
        {
            // Find possible duplicate and remove if found
            int dupeIndex = this.nodes.FindIndex(x => x.Maturity == maturity);
            if (dupeIndex != -1)
            {
                this.nodes[dupeIndex] = new CurveModelNode(maturity, value);
                return;
            }

            // Insert the node to ensure proper order
            int orderIndex = this.nodes.FindIndex(x => x.Maturity > maturity);
            if (orderIndex != -1)
            {
                this.nodes.Insert(orderIndex, new CurveModelNode(maturity, value));
            }
            else
            {
                this.nodes.Add(new CurveModelNode(maturity, value));
            }
        }

        internal void AddNode(double maturity, double value)
        {
            AddInOrder(maturity, value);
            recalculationNecessary = true;
        }

        //internal void SetNodes(IEnumerable<CurveModelNode> nodes)
        //{
        //    this.nodes.Clear();
        //    this.nodes.AddRange(nodes);
        //    this.nodes = this.nodes.OrderBy(x => x.Maturity).ToList();
        //    recalculationNecessary = true;
        //}

        internal void AddNodes(IEnumerable<Point> nodes)
        {
            foreach (var node in nodes)
            {
                AddInOrder(node.X, node.Y);
            }
            recalculationNecessary = true;
        }

        internal void ClearNodes()
        {
            this.nodes.Clear();
            recalculationNecessary = true;
        }

        internal double Get(double t)
        {
            if (nodes.Count == 0) throw new Exception("No curve nodes defined.");
            if (nodes.Count == 1) return nodes[0].Value;
            if (recalculationNecessary) Recalculate();

            if (cache.TryGetValue(t, out double o))
            {
                return o;
            }
            else
            {
                o = GetFromModel(t);
                cache.Add(t, o);
                return o;
            }
        }

        internal IEnumerable<Point> Get(double tMin, double tMax, double tStep)
        {
            for (double i = tMin; i <= tMax; i += tStep)
            {
                yield return new Point(i, Get(i));
            }
        }

        internal IEnumerable<Point> GetNodes()
        {
            foreach (var node in nodes)
            {
                yield return new Point(node.Maturity, node.Value);
            }
        }

        protected abstract double GetFromModel(double t);
    }
}
