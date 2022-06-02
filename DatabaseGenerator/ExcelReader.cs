using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Linq;
using DatabaseGenerator.Models;


namespace DatabaseGenerator
{

    class ExcelReader
    {

        public static List<T> Read<T>(string fileName, string sheetName) where T : new()
        {
            DataSet ds = ExcelReader.ReadExcelAsDataSet(fileName);
            DataTable dt = ds.Tables[sheetName];
            if (dt == null)
                throw new ApplicationException($"Cannot find sheet [{sheetName}] in excel file");

            var allColumns = dt.Columns.Cast<DataColumn>().ToList();
            var weightsColumns = allColumns.Where(c => c.ColumnName.StartsWith("W_"));

            var outList = new List<T>();

            foreach (DataRow row in dt.Rows)
            {
                T obj = new T();
                outList.Add(obj);

                if (obj is WeightedItem)
                {
                    (obj as WeightedItem).Weights = weightsColumns.Select(c => (double)row[c]).ToArray();
                }

                if (obj is Category)
                {
                    var cat = obj as Category;
                    cat.CategoryID = (int)(double)row["CategoryID"];
                    cat.CategoryName = (string)row["CategoryName"];
                    cat.PricePerCent = allColumns.Where(c => c.ColumnName.StartsWith("PPC_")).Select(name => (double)row[name]).ToArray();
                }

                if (obj is SubCategory)
                {
                    var subCat = obj as SubCategory;
                    subCat.SubCategoryID = (int)(double)row["SubCategoryID"];
                    subCat.CategoryID = (int)(double)row["CategoryID"];
                }

                if (obj is Product)
                {
                    var prod = obj as Product;
                    prod.ProductID = (int)(double)row["ProductID"];
                    prod.SubCategoryID = (int)(double)row["SubCategoryID"];
                    prod.Price = (double)row["Price"];
                    prod.Cost = (double)row["Cost"];
                }

                //if (obj is Customer)
                //{
                //    var customer = obj as Customer;
                //    customer.CustomerID = (int)(double)row["CustomerID"];
                //    customer.GeoAreaID = (int)(double)row["GeoAreaID"];
                //}

                if (obj is CustomerCluster)
                {
                    var customerCluster = obj as CustomerCluster;
                    customerCluster.ClusterID = (int)(double)row["ClusterID"];
                    customerCluster.OrdersWeight = (double)row["OrdersWeight"];
                    customerCluster.CustomersWeight = (double)row["CustomersWeight"];
                }

                if (obj is GeoArea)
                {
                    var geoArea = obj as GeoArea;
                    geoArea.GeoAreaID = (int)(double)row["GeoAreaID"];
                    geoArea.CountryCode = (geoArea.GeoAreaID == -1) ? "--" : (string)row["CountryCode"];
                }

                if (obj is Store)
                {
                    var store = obj as Store;
                    store.StoreID = (int)(double)row["StoreID"];
                    store.GeoAreaID = (int)(double)row["GeoAreaID"];
                    store.OpenDate = row["OpenDate"] is DateTime ? (DateTime?)row["OpenDate"] : null;
                    store.CloseDate = row["CloseDate"] is DateTime ? (DateTime?)row["CloseDate"] : null;
                }

                if (obj is SubCategoryLink)
                {
                    var scLink = obj as SubCategoryLink;
                    scLink.SubCategoryID = (int)(double)row["SubCategoryID"];
                    scLink.LinkedSubCategoryID = (int)(double)row["LinkedSubCategoryID"];
                    scLink.PerCent = (double)row["PerCent"];
                }
            }

            return outList;
        }



        private static DataSet ReadExcelAsDataSet(string fileName)
        {
            //  https://github.com/ExcelDataReader/ExcelDataReader

            // Required for running on .NET Core
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using (var stream = File.Open(fileName, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var config = new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                        {
                            UseHeaderRow = true
                        }
                    };

                    DataSet ds = reader.AsDataSet(config);
                    return ds;
                }
            }
        }


    }

}
