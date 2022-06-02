using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DatabaseGenerator
{
    public class DoublesArray
    {
        public double Sum { get; }

        public int Length { get { return Values.Length; } }

        public double[] Values { get; }

        public double[] ProgressiveSum { get; }

        public double this[int index] { get { return Values[index]; } }


        public DoublesArray(double[] values)
        {
            Values = values;
            Sum = values.Sum();

            ProgressiveSum = new double[values.Length];
            double s = 0;
            for (int i = 0; i < values.Length; i++)
            {
                s += values[i];
                ProgressiveSum[i] = s;
            }
        }

    }
}
