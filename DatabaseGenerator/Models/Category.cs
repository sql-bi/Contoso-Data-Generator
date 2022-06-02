using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseGenerator.Models
{
    public class Category : WeightedItem
    {
        public int CategoryID { get; set; }

        public string CategoryName { get; set; }

        public double[] PricePerCent { get; set; }
       
    }
}
