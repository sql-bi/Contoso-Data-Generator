using System;
using System.Collections.Generic;
using System.Text;


namespace DatabaseGenerator
{
    public class Config
    {
        public int OrdersCount { get; set; }
        public DateTime StartDT { get; set; }
        public int YearsCount { get; set; }
        public DateTime? CutDateBefore { get; set; }
        public DateTime? CutDateAfter { get; set; }
        public double CustomerPercentage { get; set; }
        public int CustomerFakeGenerator { get; set; }   // >0 : use fake customers

        public DaysWeight DaysWeight { get; set; }

        public double[] OrderRowsWeights { get; set; }
        public double[] OrderQuantityWeights { get; set; }
        public double[] DiscountWeights { get; set; }
        public double[] OnlinePerCent { get; set; }
        public double[] DeliveryDateLambdaWeights { get; set; }
        public Dictionary<string,string> CountryCurrency { get; set; }


        public List<AnnualSpike> AnnualSpikes { get; set; }

        public List<OneTimeSpike> OneTimeSpikes { get; set; }

        public CustomerActivity CustomerActivity { get; set; }        
    }


    public class DaysWeight
    {
        public bool DaysWeightConstant { get; set; }
        public double[] DaysWeightPoints { get; set; }
        public double[] DaysWeightValues { get; set; }
        public bool DaysWeightAddSpikes { get; set; }
        public double[] WeekDaysFactor { get; set; }

        public double DayRandomness { get; set; }
    }


    public class OneTimeSpike
    {
        public DateTime DT1 { get; set; }
        public DateTime DT2 { get; set; }
        public double Factor { get; set; }
    }


    public class AnnualSpike
    {
        public int StartDay { get; set; }
        public int EndDay { get; set; }
        public double Factor { get; set; }
    }


    public class CustomerActivity
    {
        public double[] StartDateWeightPoints { get; set; }
        public double[] StartDateWeightValues { get; set; }
        public double[] EndDateWeightPoints { get; set; }
        public double[] EndDateWeightValues { get; set; }
    }

}
