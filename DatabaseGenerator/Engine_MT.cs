using DatabaseGenerator.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;


namespace DatabaseGenerator
{

    public partial class Engine
    {

        // **********************************
        //  **** MultiThread experiments ****
        // **********************************

        private void TryMultiThread()
        {
            Parallel.For(
                0,
                _config.YearsCount * 12,
                new ParallelOptions() { MaxDegreeOfParallelism = 4 },
                monthNumber => BuildOneMonth(monthNumber));
        }



        private void BuildOneMonth(int monthNumber)
        {
            Logger.Info($"START {monthNumber}");

            //_totalDaysCount = (int)(_config.StartDT.AddYears(_config.YearsCount) 

            DateTime DT1 = _config.StartDT.AddMonths(monthNumber);
            DateTime DT2 = DT1.AddMonths(1);

            int dayNumberStart = (int)DT1.Subtract(_config.StartDT).TotalDays;
            int dayNumberEnd = (int)DT2.Subtract(_config.StartDT).TotalDays;

            string ordersFileName = Path.Combine(_outputFolder, $"temp_orders_{monthNumber}.tsv");
            string orderRowsFileName = Path.Combine(_outputFolder, $"temp_orderrows_{monthNumber}.tsv");

            Random monthRNG = new Random(monthNumber);

            using (var orderFile = new StreamWriter(ordersFileName))
            using (var orderRowsFile = new StreamWriter(orderRowsFileName))
            {
                for (int dayNumber = dayNumberStart; dayNumber < dayNumberEnd; dayNumber++)
                {
                    //Console.Write($"{dayNumber} ");

                    List<Order> todayOrders = CreateDayOrders(dayNumber, monthRNG);

                    //Console.Write($"[{dayNumber} {todayOrders.Count}]");

                    foreach (var order in todayOrders)
                    {
                        //orderFile.WriteLine(order.DumpToTsvLine());
                        foreach (var orderRow in order.Rows)
                        {
                            //  orderRowsFile.WriteLine(orderRow.DumpToTsvLine(111111));  // was order.OrderID
                        }
                    }
                }
            }

            //Console.WriteLine();

            Logger.Info($"END {monthNumber}");
        }


    }
}
