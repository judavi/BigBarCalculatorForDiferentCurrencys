using System;
using System.IO;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.API.Requests;
using cAlgo.Indicators;
using System.Collections.Generic;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class HorseCAL : Robot
    {
        [Parameter("Initial Volume", DefaultValue = 10000, MinValue = 0)]
        public int InitialVolume { get; set; }

        [Parameter("Stop Loss", DefaultValue = 0)]
        public int StopLoss { get; set; }

        [Parameter("Take Profit", DefaultValue = 0)]
        public int TakeProfit { get; set; }

        [Parameter("Threshold", DefaultValue = 4)]
        public int Threshold { get; set; }


        // All the tiemframes. Should goes from low to high
        private readonly TimeFrame[] AllTimeFrames = new TimeFrame[] 
        {
            TimeFrame.Minute5,
            TimeFrame.Minute10,
            TimeFrame.Minute15,
            TimeFrame.Minute30,
            TimeFrame.Hour
        };

        // Stores market data for each timeframe.
        public List<Dictionary<TimeFrame, MarketSeries>> Data = new List<Dictionary<TimeFrame, MarketSeries>>();

        public Dictionary<TimeFrame, MarketSeries> DataTemp = new Dictionary<TimeFrame, MarketSeries>();

        public List<double> tempValuesFromRange = new List<double>();
        public List<string> tempValuesFromRangeStr = new List<string>();




        // Stores last tick volume for each timeframe.
        private Dictionary<TimeFrame, double> LastTickVolume = new Dictionary<TimeFrame, double>();



        StreamWriter _fileWriter;
        int Tim = 1;

        int Contador = 0;

        private Symbol CAL1;
        private Symbol CAL2;
        private Symbol CAL3;
        private Symbol CAL4;
        private Symbol CAL5;
        private Symbol CAL6;
        private Symbol CAL7;

        private List<Symbol> AllSymbols { get; set; }


        protected override void OnStart()
        {
            var desktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            //getting location of user's Desktop folder  
            var filePath = Path.Combine(desktopFolder, "CADL_LOG.txt");

            _fileWriter = File.AppendText(filePath);
            //creating file
            _fileWriter.AutoFlush = true;

            Positions.Closed += OnPositionsClosed;


            CAL1 = MarketData.GetSymbol("CADCHF");
            CAL2 = MarketData.GetSymbol("CADJPY");
            CAL3 = MarketData.GetSymbol("EURCAD");
            CAL4 = MarketData.GetSymbol("GBPCAD");
            CAL5 = MarketData.GetSymbol("NZDCAD");
            CAL6 = MarketData.GetSymbol("USDCAD");
            CAL7 = MarketData.GetSymbol("AUDCAD");

            AllSymbols = new List<API.Internals.Symbol>();

            AllSymbols.Add(CAL1);
            AllSymbols.Add(CAL2);
            AllSymbols.Add(CAL3);
            AllSymbols.Add(CAL4);
            AllSymbols.Add(CAL5);
            AllSymbols.Add(CAL6);
            AllSymbols.Add(CAL7);



            foreach (var symbol in AllSymbols)
            {
                foreach (var tf in AllTimeFrames)
                {

                    DataTemp[tf] = MarketData.GetSeries(symbol, tf);
                    LastTickVolume[tf] = double.MaxValue;

                }

                Data.Add(DataTemp);
                DataTemp = new Dictionary<TimeFrame, MarketSeries>();

            }
        }

        protected override void OnStop()
        {
            _fileWriter.Close();
        }

        protected override void OnBar()
        {
            //printPos();
            foreach (var item in Data)
            {
                for (var i = 0; i < AllTimeFrames.Length - 1; i++)
                {
                    var tf = AllTimeFrames[i];
                    var data = item[tf];
                    var lastIndex = data.TickVolume.Count - 1;
                    var currentVolume = data.TickVolume[lastIndex];
                    var lastVolume = LastTickVolume[tf];
                    LastTickVolume[tf] = currentVolume;

                    //if (currentVolume < lastVolume)
                    //{
                    // New bar detected
                    var higherTf = AllTimeFrames[i + 1];
                    var higherTfData = item[higherTf];
                    HandleNewBar(data, higherTfData);
                    //}
                }

                //Print here for each symbol (5 timeframes) 
                string result = "";
                string resultTest = "";
                foreach (var valsRange in tempValuesFromRange)
                {
                    result = result + "|" + valsRange.ToString();
                }
                foreach (var valsRange in tempValuesFromRangeStr)
                {
                    resultTest = resultTest + "|" + valsRange;
                }
                //Print("{0}|{1}{2}", Server.Time.ToShortDateString() + " " + Server.Time.ToShortTimeString(), item.First().Value.SymbolCode, result);

                Print("{0}|{1}{2}", Server.Time.ToShortDateString() + " " + Server.Time.ToShortTimeString(), item.First().Value.SymbolCode, resultTest);


                tempValuesFromRange = new List<double>();
                tempValuesFromRangeStr = new List<string>();
                Contador = 0;
            }
            Tim++;
        }

        protected override void OnTick()
        {


        }

        private void HandleNewBar(MarketSeries lowerTfData, MarketSeries higherTfData)
        {
            // Getting range for previouse candle
            var range = GetBarRange(lowerTfData, 1);

            var historyIndex = 1;
            var barsCount = 0;

            // Calculating range and candles on higher timeframe
            double cumulativeRange = GetBarRange(higherTfData, historyIndex);
            while (cumulativeRange < range)
            {
                historyIndex++;
                barsCount++;
                cumulativeRange += GetBarRange(higherTfData, historyIndex);
            }

            //
            //if (barsCount >= Threshold)
            //{
            NotifyUser(lowerTfData.TimeFrame, higherTfData.TimeFrame, barsCount, range, higherTfData.SymbolCode);
            //}
        }

        private double GetBarRange(MarketSeries data, int historyIndex)
        {
            var index = data.High.Count - 2;
            var high = data.High[historyIndex];
            var low = data.Low[historyIndex];
            //var range = high - low;
            var range = high;
            return range;
        }


        private void NotifyUser(TimeFrame lowerTf, TimeFrame higherTf, int barsCount, double range, string symbolCode)
        {
            var lowerTfDefinition = lowerTf.ToString();
            var higherTfDefinition = higherTf.ToString();
            //var pipsRange = range / Symbol.PipSize;
            var pipsRange = range;
            tempValuesFromRange.Add(pipsRange);
            tempValuesFromRangeStr.Add(higherTfDefinition + " - " + pipsRange.ToString());
            //Print("Entre  vez {0}", Contador);
            Contador++;
            //Print("The recent {0} bar range has {1} pips and is greater than the last {2} of the {3} ranges, symbol {4}", lowerTfDefinition, pipsRange, barsCount, higherTfDefinition, symbolCode);
            //Print(Server.Time.Date.ToString("d") + " " + Server.Time.Date.Hour + "|" + symbolCode + "|");

        }

        private void ExecuteOrder(long volume, TradeType tradeType, Symbol S, string lab, string comm)
        {
            var result = ExecuteMarketOrder(tradeType, S, volume, lab, StopLoss, TakeProfit, null, comm);

            if (result.Error == ErrorCode.NoMoney)
                Stop();
        }

        private void OnPositionsClosed(PositionClosedEventArgs args)
        {
            Print("Closed");
            var position = args.Position;

            if (position.Label != "CADL" || position.SymbolCode != Symbol.Code)
                return;
        }
    }
}
