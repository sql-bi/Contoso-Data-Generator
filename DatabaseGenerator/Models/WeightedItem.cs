using System;
using System.Collections.Generic;
using System.Text;


namespace DatabaseGenerator.Models
{
    public abstract class WeightedItem
    {
        public double[] Weights { get; set; }       // from data file
        public double[] DaysWeight { get; set; }    // calculated
    }
}
