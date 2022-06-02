using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseGenerator.Models
{
    public class Store
    {
        public int StoreID { get; set; }
        public int GeoAreaID { get; set; }
        public string CountryCode { get; set; }   // filled from GeoArea "table"
        public DateTime? OpenDate { get; set; }
        public DateTime? CloseDate { get; set; }
    }
}
