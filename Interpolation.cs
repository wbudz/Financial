using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Financial
{
    public static class Interpolation
    {
        public static double Linear(double value, IEnumerable<KeyValuePair<double, double>> structure)
        {
            if (structure == null || structure.Count() == 0) throw new ArgumentException("Empty structure");
            if (structure.Count() == 1) return structure.First().Value;
            if (value <= structure.First().Key) return structure.First().Value;
            if (value >= structure.Last().Key) return structure.Last().Value;

            var floor = structure.Last(x => x.Key <= value);
            var cap = structure.First(x => x.Key > value);

            return floor.Value + (cap.Value - floor.Value) * (value - floor.Key) / (cap.Key - floor.Key);
        }

        public static double Linear(double value, IEnumerable<KeyValuePair<int, double>> structure)
        {
            var floatStruct = new List<KeyValuePair<double, double>>();
            foreach (var item in structure)
            {
                floatStruct.Add(new KeyValuePair<double, double>(item.Key, item.Value));
            }

            return Linear(value, floatStruct);
        }
    }
}
