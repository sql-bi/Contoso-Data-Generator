using System;
using System.Collections.Generic;
using System.Text;


namespace DatabaseGenerator.Models
{
    public class SubCategory : WeightedItem
    {
        public int SubCategoryID { get; set; }        
        public int CategoryID { get; set; }        
    }
}
