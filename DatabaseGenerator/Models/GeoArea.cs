using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseGenerator.Models
{
    public class GeoArea : WeightedItem
    {
        public int GeoAreaID { get; set; }
        public string CountryCode { get; set; }
    }
}
