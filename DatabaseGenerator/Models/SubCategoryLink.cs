using System;
using System.Collections.Generic;
using System.Text;


namespace DatabaseGenerator.Models
{
    public class SubCategoryLink
    {
        public int SubCategoryID { get; set; }
        public int LinkedSubCategoryID { get; set; }
        public double PerCent { get; set; }
    }
}
