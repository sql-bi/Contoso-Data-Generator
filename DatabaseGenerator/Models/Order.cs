using System;
using System.Collections.Generic;
using System.Text;


namespace DatabaseGenerator.Models
{

    public class Order
    {
        public long OrderID { get; set; }
        public int CustomerID { get; set; }
        public int StoreID { get; set; }
        public DateTime DT { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string CurrencyCode { get; set; }
        public List<OrderRow> Rows { get; set; }

        public string[] DumpOutputFields()
        {
            return new string[] { OrderID.ToString(), CustomerID.ToString(), StoreID.ToString(), DT.ToString("yyyy-MM-dd"), DeliveryDate.ToString("yyyy-MM-dd"), CurrencyCode };
        }

        public static string[] DumpOutputHeaders()
        {
            return new string[] { "OrderID", "CustomerID", "StoreID", "DT", "DeliveryDate","CurrencyCode" };
        }

    }

}
