using DatabaseGenerator.Models;
using System;
using System.Collections.Generic;
using System.Text;


namespace DatabaseGenerator.Fast
{

    public class CustomerListFast
    {
        private Dictionary<int, List<Customer>> _customersByGeoAreaID;


        private List<Customer> _customerList { get; set; }


        public int Count
        {
            get { return _customerList.Count; }
        }


        public CustomerListFast(List<Customer> customers)
        {
            _customerList = customers;
            CreateIndexes();
        }


        public List<Customer> FindByGeoAreaID(int geoAreaID)
        {
            if (_customersByGeoAreaID.ContainsKey(geoAreaID))
            {
                return _customersByGeoAreaID[geoAreaID];
            }
            else
            {
                return new List<Customer>();
            }
        }


        private void CreateIndexes()
        {
            _customersByGeoAreaID = new Dictionary<int, List<Customer>>();

            for (int i = 0; i < _customerList.Count; i++)
            {
                int key = _customerList[i].GeoAreaID;

                if (!_customersByGeoAreaID.ContainsKey(key))
                {
                    _customersByGeoAreaID.Add(key, new List<Customer>());
                }

                _customersByGeoAreaID[key].Add(_customerList[i]);
            }
        }

    }

}
