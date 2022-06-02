using DatabaseGenerator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DatabaseGenerator.Fast
{
    public class ProductsFast
    {

        public List<Product> Products { get; set; }

        private Dictionary<int, List<Product>> _groupingBySubCategoryID;


        public void IndexData()
        {
            _groupingBySubCategoryID = new Dictionary<int, List<Product>>();

            var q = from p in Products group p by p.SubCategoryID into newGroup orderby newGroup.Key select newGroup;

            foreach (var item in q)
            {
                _groupingBySubCategoryID.Add(item.Key, item.ToList());
            }
        }


        public List<Product> GetProductsBySubCategoryID(int subcategoryID)
        {
            if (!_groupingBySubCategoryID.ContainsKey(subcategoryID))
                return new List<Product>();

            return _groupingBySubCategoryID[subcategoryID];
        }

    }
}
