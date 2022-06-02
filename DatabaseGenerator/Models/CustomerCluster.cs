using DatabaseGenerator.Fast;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseGenerator.Models
{
    public class CustomerCluster
    {
        public int ClusterID { get; set; }
        public double OrdersWeight { get; set; }
        public double CustomersWeight { get; set; }
        

        public CustomerListFast CustomersFast { get; set; }        
    }
}
