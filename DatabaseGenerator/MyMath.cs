using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseGenerator
{

    internal class MyMath
    {
        public static double[] Interpolate(int n, double start, double end, double[] interpolationPoints, double[] interpolationValues)
        {
            // Create a sequence of equally spaced items
            var points = MathNet.Numerics.Generate.LinearSpaced(n, start, end);

            // Interpolate n elements
            var interpolation = MathNet.Numerics.Interpolate.CubicSpline(interpolationPoints, interpolationValues);
            var outVect = new double[n];
            for (int i = 0; i < n; i++)
            {
                outVect[i] = interpolation.Interpolate(points[i]);
            }

            return outVect;
        }

    }

}
