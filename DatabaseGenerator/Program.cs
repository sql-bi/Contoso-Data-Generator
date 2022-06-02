using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Windows;

namespace DatabaseGenerator
{
    class Program
    {

        static void Main(string[] args)
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName();
            Console.WriteLine($"{assemblyName.Name} - {assemblyName.Version}\n");

            if (args.Length == 0 || !Directory.Exists(args[0]))
            {
                Console.WriteLine("Usage:  databasegenerator.exe  inputfolder  outputfolder  configfile  [param:OrdersCount=nnnnnnn]");
                return;
            }

            string inputFolder = args[0];
            string outputFolder = args[1];
            string configFile = args[2];

            // read config
            var config = JsonSerializer.Deserialize<Config>(File.ReadAllText(Path.Combine(configFile)));
            InjectConfigValuesFromCommandLine(config, args);

            //////int? year = null;
            //////if (args.Length > 1)
            //////    year = int.Parse(args[1]);
            //////var t1 = new Thread(() => new Engine(dataFolder, config, 2014).Exec());
            //////var t2 = new Thread(() => new Engine(dataFolder, config, 2015).Exec());
            //////var t3 = new Thread(() => new Engine(dataFolder, config, 2016).Exec());
            //////t1.Start();
            //////t2.Start();
            //////t3.Start();
            //////t1.Join();
            //////t2.Join();
            //////t3.Join();

            new Engine(inputFolder, outputFolder, config).Exec();
        }



        private static void InjectConfigValuesFromCommandLine(Config cfg, string[] args)
        {
            Func<string[], string, string> getParamValue = (args, paramName) => { return args.Where(x => x.StartsWith($"param:{paramName}=")).Select(x => x.Split('=')[1]).FirstOrDefault(); };
            Func<string, int, int> convToInt = (s, defValue) => { return string.IsNullOrEmpty(s) ? defValue : int.Parse(s); };
            Func<string, double, double> convToDouble = (s, defValue) => { return string.IsNullOrEmpty(s) ? defValue : Double.Parse(s, CultureInfo.InvariantCulture); };
            Func<string, DateTime?, DateTime?> convToDT = (s, defValue) => { return string.IsNullOrEmpty(s) ? defValue : DateTime.ParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture); };

            cfg.OrdersCount = convToInt(getParamValue(args, "OrdersCount"), cfg.OrdersCount);
            cfg.CutDateBefore = convToDT(getParamValue(args, "CutDateBefore"), cfg.CutDateBefore);
            cfg.CutDateAfter = convToDT(getParamValue(args, "CutDateAfter"), cfg.CutDateAfter);
            cfg.CustomerPercentage = convToDouble(getParamValue(args, "CustomerPercentage"), cfg.CustomerPercentage);
        }




    }
}
