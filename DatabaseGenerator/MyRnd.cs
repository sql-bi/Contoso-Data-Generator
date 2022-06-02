using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace DatabaseGenerator
{

    public class MyRnd
    {

        /// <summary>
        /// ...
        /// </summary>
        public static int RandomIndexFromWeigthedDistribution(Random rnd, double[] weightVector)
        {
            //double vectorSum = weightVector.Sum();

            // this is faster the linq.sum()
            double vectorSum = 0;
            for (int i = 0; i < weightVector.Length; i++)
            {
                vectorSum += weightVector[i];
            }

            double x = rnd.NextDouble() * vectorSum;
            double sum = 0;

            for (int i = 0; i < weightVector.Length; i++)
            {
                sum = sum + weightVector[i];
                if (sum >= x)
                {
                    return i;
                }
            }

            throw new ApplicationException("Impossible");
        }


        /// <summary>
        /// ...
        /// </summary>
        public static int RandomIndexFromWeigthedDistribution(Random rnd, DoublesArray weightVector)
        {
            // This code seems ugly but is uses maximum performance optimization (as far as i know)

            double x = rnd.NextDouble() * weightVector.Sum;
            int vectorLen = weightVector.Length;
            double[] vector = weightVector.ProgressiveSum;

            for (int i = 0; i < vectorLen; i++)
            {
                if (vector[i] > x)
                {
                    return i;
                }
            }

            throw new ApplicationException("Impossible");
        }


    }

}
