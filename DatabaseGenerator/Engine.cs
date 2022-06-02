using CsvHelper;
using DatabaseGenerator.Fast;
using DatabaseGenerator.Models;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace DatabaseGenerator
{

    public partial class Engine
    {

        private const string ORDERS_FILENAME = "orders.csv";
        private const string ORDER_ROWS_FILENAME = "orderRows.csv";
        private const string CUSTOMERSCLUSTERS_FILENAME = "customersClusters.csv";
        private const string LOG_FILENAME = "log.log";


        private Config _config;
        private int _totalDaysCount;
        private long _orderNumberDailyRange;
        private string _inputFolder;
        private string _outputFolder;
        private string _ordersFileFP;
        private string _orderRowsFileFP;

        private CultureInfo _outputCultureInfo;


        private double[] _daysWeight;
        private List<Category> categories;
        private List<SubCategory> subcategories;
        private List<Product> products;
        private List<GeoArea> geoAreas;
        private List<CustomerCluster> customerClusters;
        private List<Customer> customers;
        private CustomerListFast customersListFast;
        private ProductsFast productsFast;
        private List<Store> stores;
        private List<SubCategoryLink> subCatLinks;
        private double[] OnlineDailyPercentages;
        private double[] DeliveryDateDayLambdas;

        private int _CounterCannotAssignStore = 0;
        private int _CounterOnlineStore = 0;


        /// <summary>
        /// ...
        /// </summary>
        public Engine(String inputfolder, string outputFolder, Config config)
        {
            _config = config;
            _inputFolder = inputfolder;
            _outputFolder = outputFolder;
            _ordersFileFP = Path.Combine(outputFolder, ORDERS_FILENAME);
            _orderRowsFileFP = Path.Combine(outputFolder, ORDER_ROWS_FILENAME);
            _totalDaysCount = (int)(_config.StartDT.AddYears(_config.YearsCount).Subtract(_config.StartDT).TotalDays);
            _orderNumberDailyRange = (long)Math.Pow(10, Math.Ceiling(Math.Log10(_config.OrdersCount / _totalDaysCount)) + 1);

            _outputCultureInfo = new CultureInfo(String.Empty);             // Creates an InvariantCulture that can be modified
            _outputCultureInfo.NumberFormat.NumberDecimalSeparator = ".";   // ...to be sure  :)
        }


        /// <summary>
        /// ...
        /// </summary>
        public void Exec()
        {
            DateTime startDT = DateTime.UtcNow;

            // - RandomNumberGenerator -
            // DO NOT CHANGE SEED !!!
            // Do not not use a static global rng: it's not supported in multithread
            Random rng = new Random(0);

            Logger.Init(Path.Combine(_outputFolder, LOG_FILENAME));

            try
            {
                var exeAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                var productName = exeAssembly
                    .GetCustomAttributes(typeof(System.Reflection.AssemblyProductAttribute), false)
                    .Cast<System.Reflection.AssemblyProductAttribute>()
                    .FirstOrDefault()
                    ?.Product;

                Logger.Info("START");
                Logger.Info($"Ver: {exeAssembly.GetName().Version.ToString()}");
                Logger.Info($"Info: {productName}");
                Logger.Info($"InputFolder  : {_inputFolder}");
                Logger.Info($"OutputFolder : {_outputFolder}");

                // some calculation on config data
                PrepareConfigData();

                Logger.Info($"StartDT      : {_config.StartDT}");
                Logger.Info($"TotalDays    : {_totalDaysCount}");
                Logger.Info($"O_N_D_Range  : {_orderNumberDailyRange}");
                Logger.Info($"CutDateBefore: {_config.CutDateBefore}");
                Logger.Info($"CutDateAfter : {_config.CutDateAfter}");
                Logger.Info($"OrdersCount  : {_config.OrdersCount}");
                Logger.Info($"{_ordersFileFP}");
                Logger.Info($"{_orderRowsFileFP}");

                // delete existing output files
                PrepareWorkingFolder();

                // Read data from excel file
                Logger.Info($"Read input data");
                ReadInputData(rng);
                Logger.Info($"CustomersCount : {customers.Count}");

                // Pre-calculate daily weights for categories, subcategories, etc... and other data
                Logger.Info($"Prepare input data");
                PrepareInputData(rng);

                // Days weights
                CalculateDaysWeight(_config.YearsCount, _totalDaysCount, _config.StartDT);

                // Create Orders and Order-Rows
                int logOrderCounter = 0;
                int logOrderRowCounter = 0;
                using (var orderFile = new StreamWriter(_ordersFileFP))
                using (var orderRowsFile = new StreamWriter(_orderRowsFileFP))
                {
                    // tsv headers
                    orderFile.WriteLine(String.Join(",", Order.DumpOutputHeaders()));
                    orderRowsFile.WriteLine(String.Join(",", OrderRow.DumpOutputHeaders()));

                    // rows 
                    for (int dayNumber = 0; dayNumber < _totalDaysCount; dayNumber++)
                    {
                        if (dayNumber % (_totalDaysCount / 100) == 0)
                            Logger.Info($"{100 * dayNumber / _totalDaysCount}%  Orders: {logOrderCounter}");

                        //if (_onlyOneYear.HasValue)
                        //    if (_config.StartDT.AddDays(dayNumber).Year != _onlyOneYear.Value)
                        //        continue;

                        List<Order> todayOrders = CreateDayOrders(dayNumber, rng);

                        foreach (var order in todayOrders.Where(x => x.DT >= _config.CutDateBefore && x.DT <= _config.CutDateAfter))
                        {
                            orderFile.WriteLine(String.Join(",", order.DumpOutputFields()));
                            logOrderCounter++;

                            foreach (var orderRow in order.Rows)
                            {
                                orderRowsFile.WriteLine(String.Join(",", orderRow.DumpOutputFields(order.OrderID, _outputCultureInfo)));
                                logOrderRowCounter++;
                            }
                        }
                    }
                }

                Logger.Info($"Orders:            {logOrderCounter}");
                Logger.Info($"Online orders:     {_CounterOnlineStore}");
                Logger.Info($"OrdersRows:        {logOrderRowCounter}");
                Logger.Info($"CannotAssignStore: {_CounterCannotAssignStore}");
                Logger.Info($"Elapsed:           {DateTime.UtcNow.Subtract(startDT).TotalSeconds.ToString("0.000")}");
                Logger.Info("THE END");
            }
            catch (Exception ex)
            {
                Logger.Info($"EXCEPTION: {ex.ToString()}");
                throw;
            }
        }





        /// <summary>
        /// ...
        /// </summary>
        private List<Order> CreateDayOrders(int dayNumber, Random monthRNG)
        {
            var today = _config.StartDT.AddDays(dayNumber);
            int transactionsToday = (int)((double)_config.OrdersCount * _daysWeight[dayNumber] / _daysWeight.Sum());
            transactionsToday = monthRNG.Next((int)(transactionsToday * (1.0 - _config.DaysWeight.DayRandomness)),
                                              (int)(transactionsToday * (1.0 + _config.DaysWeight.DayRandomness)));

            var todayOrders = new List<Order>();

            for (int i = 0; i < transactionsToday; i++)
            {
                long orderNumber = (dayNumber + 1) * _orderNumberDailyRange + i;

                var order = BuildOrder(orderNumber, today, dayNumber, monthRNG);

                if (order != null)
                {
                    todayOrders.Add(order);
                }
                else
                {
                    //nullCounter++;
                }
            }

            return todayOrders;
        }


        /// <summary>
        /// ...
        /// </summary>
        private Order BuildOrder(long orderNumber, DateTime dt, int dayNumber, Random RNG)
        {

            // ------------------------------------------
            // CustomerCluster
            CustomerCluster customerCluster;
            {
                double[] ccOrderWeights = customerClusters.Select(cc => cc.OrdersWeight).ToArray();
                var index = MyRnd.RandomIndexFromWeigthedDistribution(RNG, ccOrderWeights);
                customerCluster = customerClusters[index];
            }

            // ------------------------------------------
            // GeoArea
            GeoArea geoArea;
            {
                double[] weights = geoAreas.Select(x => x.DaysWeight[dayNumber]).ToArray();
                int index = MyRnd.RandomIndexFromWeigthedDistribution(RNG, weights);
                geoArea = geoAreas[index];
            }

            // ------------------------------------------
            // Customer
            Customer customer = null;
            {
                var customerSubGroup = customerCluster.CustomersFast.FindByGeoAreaID(geoArea.GeoAreaID);

                // OLD (too slow)
                //customerSubGroup = customerSubGroup.Where(c => dt > c.StartDT && dt < c.EndDT).ToList();
                //if (customerSubGroup.Count == 0)
                //    return null;   // <--- exit ???
                //customer = customerSubGroup[RNG.Next(customerSubGroup.Count)];

                // NEW  (this is a lot faster, although is less precise)
                //      Random pick of customers from subgroup
                if (customerSubGroup.Count > 0)
                {
                    for (int i = 0; i < 100; i++)
                    {
                        var tempCustomer = customerSubGroup[RNG.Next(customerSubGroup.Count)];
                        if (dt > tempCustomer.StartDT && dt < tempCustomer.EndDT)
                        {
                            customer = tempCustomer;
                            break;
                        }
                    }
                }

                if (customer == null)
                {
                    return null;
                }
            }

            // ------------------------------------------
            // Store
            Store store = null;
            {
                double todayOnlinePercent = OnlineDailyPercentages[dayNumber];

                if (RNG.NextDouble() < (1.0 - todayOnlinePercent))
                {
                    // Try to find an open store in the same GeoArea
                    var storesAvailable = stores
                        .Where(s => s.GeoAreaID == customer.GeoAreaID)
                        .Where(s => s.OpenDate == null || s.OpenDate.Value <= dt)
                        .Where(s => s.CloseDate == null || s.CloseDate >= dt)
                        .ToList();

                    if (storesAvailable.Count > 0)
                    {
                        store = storesAvailable[RNG.Next(storesAvailable.Count)];
                    }
                    else
                    {
                        // Try to find an open store in the same CountryCode
                        List<Store> alternativeStores = stores
                                        .Where(x => x.CountryCode == geoArea.CountryCode)
                                        .Where(s => s.OpenDate == null || s.OpenDate.Value <= dt)
                                        .Where(s => s.CloseDate == null || s.CloseDate >= dt)
                                        .ToList();

                        if (alternativeStores.Count > 0)
                        {
                            store = alternativeStores[RNG.Next(alternativeStores.Count)];
                        }
                        else
                        {
                            // do nothing
                            _CounterCannotAssignStore++;
                        }
                    }
                }

                if (store == null)
                {
                    store = stores.Single(s => s.GeoAreaID == -1);  // online store
                    _CounterOnlineStore++;
                }
            }

            // ------------------------------------------
            // Order
            var order = new Order()
            {
                OrderID = orderNumber,
                CustomerID = customer.CustomerID,
                StoreID = store.StoreID,
                DT = dt,
                DeliveryDate = dt,
                CurrencyCode = _config.CountryCurrency[geoArea.CountryCode],
                Rows = new List<OrderRow>()
            };

            // recalculate delivery date for online orders
            if (store.GeoAreaID == -1)
            {
                var deltaDays = 1 + MathNet.Numerics.Distributions.Poisson.Sample(RNG, DeliveryDateDayLambdas[dayNumber]);
                order.DeliveryDate = dt.AddDays(deltaDays);
            }

            // ------------------------------------------
            // Order-Rows
            int numberOfRows = 1 + MyRnd.RandomIndexFromWeigthedDistribution(RNG, _config.OrderRowsWeights);
            for (int rowNumber = 0; rowNumber < numberOfRows; rowNumber++)
            {
                var orderRow = BuildOrderRow(rowNumber, dayNumber, null, RNG);
                order.Rows.Add(orderRow);

                // add related products?
                var linkedSubCategoris = subCatLinks.Where(sc => sc.SubCategoryID == orderRow.SubCategoryID).ToList();
                foreach (var linkedSubCat in linkedSubCategoris)
                {
                    if (RNG.NextDouble() < linkedSubCat.PerCent)
                    {
                        // add a related product
                        rowNumber++;
                        var orderRow2 = BuildOrderRow(rowNumber, dayNumber, linkedSubCat.LinkedSubCategoryID, RNG);
                    }
                }
            }

            return order;
        }


        /// <summary>
        /// ...
        /// </summary>
        private OrderRow BuildOrderRow(int rowNumber, int dayNumber, int? fixedSubcategory, Random RNG)
        {
            Category category = null;
            SubCategory subCategory = null;

            if (fixedSubcategory.HasValue)
            {
                subCategory = subcategories.Single(sc => sc.SubCategoryID == fixedSubcategory);
                category = categories.Single(c => c.CategoryID == subCategory.CategoryID);
            }

            // ------------------------------------------
            // Category
            if (category == null)
            {
                double[] weights = categories.Select(cat => cat.DaysWeight[dayNumber]).ToArray();
                int index = MyRnd.RandomIndexFromWeigthedDistribution(RNG, weights);
                category = categories[index];
            }

            // ------------------------------------------
            // Subcatecory
            if (subCategory == null)
            {
                var availableSubCategories = subcategories.Where(x => x.CategoryID == category.CategoryID).ToList();
                double[] subcategories_today_weights = availableSubCategories.Select(sc => sc.DaysWeight[dayNumber]).ToArray();
                int index = MyRnd.RandomIndexFromWeigthedDistribution(RNG, subcategories_today_weights);
                subCategory = availableSubCategories[index];
            }

            // ------------------------------------------
            // Product
            Product product;
            {
                var availableProcuts = productsFast.GetProductsBySubCategoryID(subCategory.SubCategoryID);
                double[] products_today_weights = availableProcuts.Select(p => p.DaysWeight[dayNumber]).ToArray();
                int index = MyRnd.RandomIndexFromWeigthedDistribution(RNG, products_today_weights);
                product = availableProcuts[index];
            }

            // ------------------------------------------
            // Quantity
            int qty = 1 + MyRnd.RandomIndexFromWeigthedDistribution(RNG, _config.OrderQuantityWeights);

            // ------------------------------------------
            // Price and discount
            double unitPrice = product.Price * category.PricePerCent[category.PricePerCent.Length * dayNumber / _totalDaysCount];
            double unitCost = product.Cost * category.PricePerCent[category.PricePerCent.Length * dayNumber / _totalDaysCount];

            double discount = (double)MyRnd.RandomIndexFromWeigthedDistribution(RNG, _config.DiscountWeights);
            double netPrice = unitPrice * (1.0 - discount / 100.0);

            // Create the row
            var orderRow = new OrderRow()
            {
                RowNumber = rowNumber,
                Quantity = qty,
                CategoryID = category.CategoryID,
                SubCategoryID = subCategory.SubCategoryID,
                ProductID = product.ProductID,
                UnitPrice = (decimal)unitPrice,
                NetPrice = (decimal)netPrice,
                UnitCost = (decimal)unitCost,
            };

            return orderRow;
        }











        /*
        /// <summary>
        /// Build an order
        /// </summary>
        private static string BuildOrder(int i, DateTime dt, int dayNumber)
        {

            // Category
            double[] new_categories_day_weight = categories.Select(cat => cat.DaysWeight[dayNumber]).ToArray();
            //OLD: int categoryIDX = MyRnd.IndexFromWeigthedDistribution(rng, categoryWeights);
            int categoryIDX = MyRnd.RandomIndexFromWeigthedDistribution(rng, new_categories_day_weight);
            int categoryID = categories[categoryIDX].CategoryID;

            // Subcategory
            var availableSubCategories = subcategories.Where(x => x.CategoryID == categoryID).ToList();
            double[] subcategories_today_weights = availableSubCategories.Select(sc => sc.DaysWeight[dayNumber]).ToArray();
            //double[] subCategoryWeights = availableSubCategories.Select(x => x.Weight).ToArray();
            int subCategoryIDX = MyRnd.RandomIndexFromWeigthedDistribution(rng, subcategories_today_weights);
            int subCategoryID = availableSubCategories[subCategoryIDX].SubCategoryID;

            // Product
            //var localProducts = products.Where(x => x.SubCategoryID == subCategoryID).ToList();   // slow
            var availableProducts = productsFast.GetProductsBySubCategoryID(subCategoryID);  // fast
            if (availableProducts.Count == 0)
                throw new ApplicationException("Cannot find a related product");
            //var productWeights = availableProducts.Select(x => x.Weight).ToArray();
            var products_today_weights = availableProducts.Select(p => p.DaysWeight[dayNumber]).ToArray();
            int productIDX = MyRnd.RandomIndexFromWeigthedDistribution(rng, products_today_weights);
            int productID = availableProducts[productIDX].ProductID;

            // Customer (OLD WAY)
            //int customerIDX = MyRnd.RandomIndexFromWeigthedDistribution(rng, TEMP_customerWeights);
            //int customerID = customers[customerIDX].CustomerID;


            // Customer (NEW WAY)
            double[] ccweights = customerClusters.Select(cc => cc.OrdersWeight).ToArray();
            var ccIndex = MyRnd.RandomIndexFromWeigthedDistribution(rng, ccweights);
            var customerCluster = customerClusters[ccIndex];
            int ii = rng.Next(0, customerCluster.Customers.Count);
            var customer = customerCluster.Customers[ii];

            // select a random Cluster using Cluster.OrderWeight
            // select a random Customer from previusly selected cluster
            // assign the order

            // Store: select from the same region of customer or from online


            // Create TSV line
            string catID = categoryID.ToString();
            string subCatID = subCategoryID.ToString();
            string custClustID = customerCluster.ClusterID.ToString();
            string custID = customer.CustomerID.ToString();
            string PID = productID.ToString();
            int qty = 1 + rng.Next(4);
            qty = 1;
            string line = $"{i}\t{dt.ToString("O")}\t{catID}\t{subCatID}\t{custClustID}\t{custID}\t{PID}\t{qty}";

            return line;
        }
        */

    }

}
