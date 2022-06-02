using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;


namespace DatabaseGenerator.Models
{

    public class Product : WeightedItem
    {
        public int ProductID { get; set; }
        public int SubCategoryID { get; set; }
        public double Price { get; set; }
        public double Cost { get; set; }
    }    

}
