using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;


namespace DatabaseGenerator.Models
{

    public class OrderRow
    {
        public int RowNumber { get; set; }  // required?
        public int CategoryID { get; internal set; }
        public int SubCategoryID { get; internal set; }
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal NetPrice { get; set; }
        public decimal UnitCost { get; set; }

        public string[] DumpOutputFields(long orderID, CultureInfo ci)
        {
            return new string[]
            {
                orderID.ToString(),
                RowNumber.ToString(),
                ProductID.ToString(),
                Quantity.ToString(),
                UnitPrice.ToString(ci),
                NetPrice.ToString(ci),
                UnitCost.ToString(ci)
            };
        }

        public static string[] DumpOutputHeaders()
        {
            return new string[]
            {
                "OrderID",
                "RowNumber",
                "ProductID",
                "Quantity",
                "UnitPrice",
                "NetPrice",
                "UnitCost"
            };
        }

    }

}
