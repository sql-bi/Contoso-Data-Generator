using System;
using System.Collections.Generic;
using System.Text;


namespace DatabaseGenerator.Models
{

    public class Customer
    {
        public int CustomerID { get; set; }
        public int GeoAreaID { get; set; }
        public DateTime StartDT { get; set; }
        public DateTime EndDT { get; set; }
    }

}
