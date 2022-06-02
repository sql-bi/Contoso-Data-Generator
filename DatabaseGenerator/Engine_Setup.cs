using DatabaseGenerator.Fast;
using DatabaseGenerator.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace DatabaseGenerator
{

    public partial class Engine
    {

        /// <summary>
        /// ...
        /// </summary>
        private void PrepareConfigData()
        {
            if (_config.CutDateBefore == null) _config.CutDateBefore = _config.StartDT;
            if (_config.CutDateAfter == null) _config.CutDateAfter = _config.StartDT.AddYears(_config.YearsCount);
        }



        /// <summary>
        /// ...
        /// </summary>
        private void PrepareWorkingFolder()
        {
            if (File.Exists(_ordersFileFP))
                File.Delete(_ordersFileFP);

            if (File.Exists(_orderRowsFileFP))
                File.Delete(_orderRowsFileFP);

        }


        /// <summary>
        /// ...
        /// </summary>
        private void ReadInputData(Random rng)
        {
            //Directory.CreateDirectory(Path.Combine(_workingFolder, "_temp"));

            string excelFileName = Path.Combine(_inputFolder, "data.xlsx");

            //string excelFileName2 = Path.Combine(_workingFolder, "_temp/_" + Guid.NewGuid().ToString() + ".xlsx");
            //File.Copy(excelFileName, excelFileName2);
            //excelFileName = excelFileName2;


            categories = ExcelReader.Read<Category>(excelFileName, "categories");
            subcategories = ExcelReader.Read<SubCategory>(excelFileName, "subcategories");
            products = ExcelReader.Read<Product>(excelFileName, "products");
            customerClusters = ExcelReader.Read<CustomerCluster>(excelFileName, "customerClusters");
            geoAreas = ExcelReader.Read<GeoArea>(excelFileName, "geoareas");
            stores = ExcelReader.Read<Store>(excelFileName, "stores");
            subCatLinks = ExcelReader.Read<SubCategoryLink>(excelFileName, "subcatlinks");

            if (_config.CustomerFakeGenerator > 0)
            {
                customers = new List<Customer>();
                int geoAreasCount = geoAreas.Count;
                for (int i = 0; i < _config.CustomerFakeGenerator; i++)
                {
                    customers.Add(
                        new Customer()
                        {
                            CustomerID = i + 1,
                            GeoAreaID = geoAreas[rng.Next(1, geoAreasCount)].GeoAreaID
                        });
                }
            }
            else
            {
                customers = ReadCustomersRptFile(Path.Combine(_inputFolder, "customers.rpt"));                
                // shuffle customers
                var localRnd = new Random(0);
                customers = customers.OrderBy(x => localRnd.Next()).ToList();
                // get a percentage of customers
                int topN = (int)(customers.Count * _config.CustomerPercentage);
                customers = customers.Take(topN).ToList();               
            }
        }


        /// <summary>
        /// ...
        /// </summary>
        public List<Customer> ReadCustomersRptFile(string rptFleName)
        {
            var outList = new List<Customer>();

            using (var rptFile = new StreamReader(rptFleName))
            {
                rptFile.ReadLine();  // ignore headers (first line)
                rptFile.ReadLine();  // ignore headers (second line)

                string line;
                while ((line = rptFile.ReadLine()) != null)
                {
                    if (!String.IsNullOrWhiteSpace(line))
                    {
                        string[] items = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (items.Length == 2 && items[1] != "NULL")
                        {
                            outList.Add(
                                new Customer()
                                {
                                    CustomerID = int.Parse(items[0]),
                                    GeoAreaID = int.Parse(items[1])
                                });
                        }
                    }
                }
            }

            return outList;
        }


        /// <summary>
        /// ...
        /// </summary>
        private void PrepareInputData(Random rng)
        {
            // fill "countrycode" in store
            FillStoreCountryCode();

            // setup "product fast"
            productsFast = new ProductsFast() { Products = products };
            productsFast.IndexData();

            // calculate daily weights
            CalculateDailyWeights(categories.ToArray());
            CalculateDailyWeights(subcategories.ToArray());
            CalculateDailyWeights(products.ToArray());
            CalculateDailyWeights(geoAreas.ToArray());

            // add Start_date and End_date to customers
            AddStartEndDateToCustomers(rng);

            // setup "customer fast"
            customersListFast = new CustomerListFast(customers);

            // distribute customers across clusters
            DistributeCustomersAmongClusters(rng);

            // online probability distribution on each day
            OnlineDailyPercentages = CalculateDailyWeights(_config.OnlinePerCent);

            // lambda for Poisson distr. of Delivery Date
            DeliveryDateDayLambdas = CalculateDailyWeights(_config.DeliveryDateLambdaWeights);
        }


        private void FillStoreCountryCode()
        {
            foreach (var store in stores)
            {
                var geoArea = geoAreas.Single(x => x.GeoAreaID == store.GeoAreaID);
                store.CountryCode = geoArea.CountryCode;
            }
        }



        /// <summary>
        /// ...
        /// </summary>
        private void DistributeCustomersAmongClusters(Random rng)
        {
            // temp lists
            var tempCustomersPerCluster = new List<Customer>[customerClusters.Count];
            for (int i = 0; i < customerClusters.Count; i++)
            {
                tempCustomersPerCluster[i] = new List<Customer>();
            }

            // distribute
            double[] weights = customerClusters.Select(cc => cc.CustomersWeight).ToArray();
            for (int i = 0; i < customers.Count; i++)
            {
                int index = MyRnd.RandomIndexFromWeigthedDistribution(rng, weights);
                tempCustomersPerCluster[index].Add(customers[i]);
            }

            // setup fast-search
            for (int i = 0; i < customerClusters.Count; i++)
            {
                CustomerCluster cc = customerClusters[i];
                cc.CustomersFast = new CustomerListFast(tempCustomersPerCluster[i]);
                Logger.Info($"Cluster: {cc.ClusterID}  ({cc.CustomersWeight}) --> {cc.CustomersFast.Count}");
            }

            // dump clusters-customers tsv
            using (var outFile = new StreamWriter(Path.Combine(_outputFolder, CUSTOMERSCLUSTERS_FILENAME)))
            {
                for (int i = 0; i < customerClusters.Count; i++)
                {
                    var cc = customerClusters[i];
                    var custs = tempCustomersPerCluster[i];

                    for (int j = 0; j < custs.Count; j++)
                    {
                        outFile.WriteLine($"{cc.ClusterID},{custs[j].CustomerID}");
                    }
                }
            }
        }


        /// <summary>
        /// ...
        /// </summary>
        private void AddStartEndDateToCustomers(Random rng)
        {
            Logger.Info("AddStartEndDateToCustomers - START");

            DateTime startDT1 = new DateTime((int)_config.CustomerActivity.StartDateWeightPoints.First(), 1, 1);
            DateTime startDT2 = new DateTime((int)_config.CustomerActivity.StartDateWeightPoints.Last(), 12, 31);

            DateTime endDT1 = new DateTime((int)_config.CustomerActivity.EndDateWeightPoints.First(), 1, 1);
            DateTime endDT2 = new DateTime((int)_config.CustomerActivity.EndDateWeightPoints.Last(), 12, 31);

            double startDaysCount = startDT2.Subtract(startDT1).TotalDays;
            double endDaysCount = endDT2.Subtract(endDT1).TotalDays;

            var startDaysWeight = MyMath.Interpolate((int)startDaysCount,
                                                         _config.CustomerActivity.StartDateWeightPoints.First(),
                                                         _config.CustomerActivity.StartDateWeightPoints.Last(),
                                                         _config.CustomerActivity.StartDateWeightPoints,
                                                         _config.CustomerActivity.StartDateWeightValues);

            var endDaysWeight = MyMath.Interpolate((int)endDaysCount,
                                                         _config.CustomerActivity.EndDateWeightPoints.First(),
                                                         _config.CustomerActivity.EndDateWeightPoints.Last(),
                                                         _config.CustomerActivity.EndDateWeightPoints,
                                                         _config.CustomerActivity.EndDateWeightValues);

            DoublesArray startDaysWeight_Fast = new DoublesArray(startDaysWeight);
            DoublesArray endDaysWeight_Fast = new DoublesArray(endDaysWeight);

            int customerCount = customers.Count;

            // to increase speed, process group of customers (1M --> groups of 10 customers)
            int step = customerCount / 100000;
            if (step == 0)
                step = 1;

            for (int i = 0; i < customerCount; i += step)
            {
                DateTime startDT = startDT1.AddDays(MyRnd.RandomIndexFromWeigthedDistribution(rng, startDaysWeight_Fast));
                DateTime endDT = endDT1.AddDays(MyRnd.RandomIndexFromWeigthedDistribution(rng, endDaysWeight_Fast));

                if (startDT > endDT)
                {
                    DateTime swapDT = startDT;
                    startDT = endDT;
                    endDT = swapDT;
                }

                for (int n = 0; n < step; n++)
                {
                    int j = i + n;

                    if (j < customerCount)
                    {
                        customers[j].StartDT = startDT;
                        customers[j].EndDT = endDT;
                    }
                }
            }

            Logger.Info("AddStartEndDateToCustomers - END");
        }


        /// <summary>
        /// ...
        /// </summary>
        private void CalculateDailyWeights(WeightedItem[] items)
        {
            foreach (var item in items)
            {
                item.DaysWeight = CalculateDailyWeights(item.Weights);
            }
        }


        /// <summary>
        /// ...
        /// </summary>
        private double[] CalculateDailyWeights(double[] weights)
        {
            int slicesCount = weights.Length;
            double[] points = MathNet.Numerics.Generate.LinearSpaced(slicesCount, 0, slicesCount);
            return MyMath.Interpolate(_totalDaysCount, 0, slicesCount, points, weights);
        }


        /// <summary>
        /// ...
        /// </summary>
        private void CalculateDaysWeight(int yearsCount, int daysCount, DateTime startDT)
        {
            if (_config.DaysWeight.DaysWeightConstant)
                _config.DaysWeight.DaysWeightValues = Enumerable.Repeat(1.0, _config.DaysWeight.DaysWeightPoints.Length).ToArray();

            // basic curve
            var daysWeight = MyMath.Interpolate(daysCount,
                                                _config.DaysWeight.DaysWeightPoints.First(),
                                                _config.DaysWeight.DaysWeightPoints.Last(),
                                                _config.DaysWeight.DaysWeightPoints,
                                                _config.DaysWeight.DaysWeightValues);

            // add some spike and low/high period
            if (_config.DaysWeight.DaysWeightAddSpikes)
            {
                if (_config.AnnualSpikes != null)
                {
                    foreach (var annualSpike in _config.AnnualSpikes)
                    {
                        for (int y = 0; y < yearsCount; y++)
                        {
                            int arrayDayShift = (int)(new DateTime(startDT.Year + y, 1, 1).Subtract(startDT).TotalDays);

                            for (int day = annualSpike.StartDay; day < annualSpike.EndDay; day++)
                            {
                                double x = 1 + annualSpike.Factor * Math.Sin(Math.PI / (annualSpike.EndDay - annualSpike.StartDay) * (day - annualSpike.StartDay));
                                if (x < 0)
                                    x = 0;

                                int index = arrayDayShift + day;
                                if (index < daysWeight.Length)
                                {
                                    daysWeight[index] *= x;
                                }
                            }
                        }
                    }
                }

                if (_config.OneTimeSpikes != null)
                {
                    foreach (var oneTimeSpike in _config.OneTimeSpikes)
                    {
                        int days = (int)oneTimeSpike.DT2.Subtract(oneTimeSpike.DT1).TotalDays + 1;
                        int baseShift = (int)oneTimeSpike.DT1.Subtract(startDT).TotalDays;

                        for (int i = 0; i < days; i++)
                        {
                            // calculate the multiplication factor. 
                            // (days+1) and (i+1) required to create the spike also for one-day-only spike.
                            double xFactor = 1 + (oneTimeSpike.Factor - 1.0) * Math.Sin(Math.PI / (days + 1) * (i + 1));

                            int index = baseShift + i;
                            if (index >= 0 && index < daysWeight.Length)
                            {
                                daysWeight[index] *= xFactor;
                            }
                        }
                    }
                }

                // weekday weights
                DateTime currentDay = startDT;
                for (int i = 0; i < daysWeight.Length; i++)
                {
                    daysWeight[i] *= _config.DaysWeight.WeekDaysFactor[(int)currentDay.DayOfWeek];
                    currentDay = currentDay.AddDays(1);
                }
            }

            _daysWeight = daysWeight;
        }


    }

}
