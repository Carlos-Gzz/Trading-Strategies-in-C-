//Find attached the ReadMe file.
//Here goes the default cAlgo libraries.
using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class cAlgoTradingStrategyinDepth : Robot
    {
//In here we start specifying the Parameters we want to be able to easy-set. (I talk about the easy-set in the ReadMe file).
        [Parameter("Source")]
        public DataSeries Source { get; set; }
//For example, lets set some indicators used by Larry Connors, such as RSI and SMA.
        //RSI Periods.
        [Parameter("Periods", DefaultValue = 2)]
        public int Periods { get; set; }
        //Here we can set the lots used by the bot.
        [Parameter("Quantity (Lots)", DefaultValue = 0.25, MinValue = 0.01, Step = 0.01)]
        public double Quantity { get; set; }

        //SMA 200 mostly used to get the trend of the pair.
        [Parameter("SMA Period", DefaultValue = 200)]
        public int SmaPeriod { get; set; }

        //Second SMA to get change signals.
        [Parameter("SMA2 Period", DefaultValue = 5)]
        public int Sma2Period { get; set; }

//Here i set the trailing stop and the trailing trigger. (Go into the ReadMe file for in depth information).
        [Parameter("trigger ", DefaultValue = 20)]
        public int Trigger { get; set; }

        [Parameter("Trailing", DefaultValue = 9)]
        public int Trailing { get; set; }
        //End of TS

//Stop Loss and Top Profit.
        [Parameter("Stop Loss (pips)", DefaultValue = 20)]
        public int StopLoss { get; set; }

        [Parameter("Top Profit (pips)", DefaultValue = 80)]
        public int TopProfit { get; set; }
//SL y TP

        private RelativeStrengthIndex rsi;
        private SimpleMovingAverage sma;
        private SimpleMovingAverage sma2;


        // Initialization logic
        protected override void OnStart()
        {
            rsi = Indicators.RelativeStrengthIndex(Source, Periods);
            sma = Indicators.SimpleMovingAverage(Source, SmaPeriod);
            sma2 = Indicators.SimpleMovingAverage(Source, Sma2Period);
        }

        // The trading strategy. I called both things in here, the Trailing method (in order to call the trailing stop and trailing trigger),
        // And the strategy. I mostly did it this way, so the 'skeleton' of the programming can be used/changed/modified easier even with a change in strategy.
        protected override void OnTick()
        {
            // As you can see in the line above it says "protected override void OnTick()" by OnTick it means that in each Tick the bot is going to run the given condition.
            // But in the specific case you prefer the condition to be checked at the end of each Bar (as a complete candle-bar), change the OnTick() to OnBar().
            TRAILING();
            {
                ExecuteStrategy();
            }
        }
        // Lets give any random condition (this is not any of my real conditions/strategies, it's only informative to show how to automate a strategy in cAlgo).
        private void ExecuteStrategy()
        {
            if (MarketSeries.Close.Last(1) > sma.Result.Last(1))
            {
                Close(TradeType.Sell);
                Open(TradeType.Buy);

            }
            else if (MarketSeries.Close.Last(1) < sma.Result.Last(1))
            {
                Close(TradeType.Buy);
                Open(TradeType.Sell);
            }
        }
        // The Close and Open conditions are given:
        private void Close(TradeType tradeType)
        {
            foreach (var position in Positions.FindAll("Bot-Name", Symbol, tradeType))
                ClosePosition(position);
            // I ask it to close any given positions of the Bot-Name, specified Symbol and tradeType (Buy or Sell accordingly).
        }

        private void Open(TradeType tradeType)
        {
            var position = Positions.Find("Bot-Name", Symbol, tradeType);
            var volumeInUnits = Symbol.QuantityToVolumeInUnits(Quantity);

            if (position == null)
                // IMPORTANT: It is important to add the condition in OpenTrade as to if position == null; otherwise the given the strategy condition True,
                // the bot would keep Opening trades until it will make the account lose its margin.
                ExecuteMarketOrder(tradeType, Symbol, volumeInUnits, "Bot-Name", StopLoss, TopProfit);
        }
        //Trailing and Trigger conditions.
        private void TRAILING()
        {
            if (Trailing > 0 && Trigger > 0)
            {

                Position[] positions = Positions.FindAll("Bot-Name", Symbol);
                //Include all positions (either Buy or Sell).
                foreach (Position position in positions)
                {

                    if (position.TradeType == TradeType.Sell)
                    {

                        double distance = position.EntryPrice - Symbol.Ask;

                        if (distance >= Trigger * Symbol.PipSize)
                        {

                            double newStopLossPrice = Symbol.Ask + Trailing * Symbol.PipSize;
                            // It is important to add the Symbol.PipSize, this way this code can apply to any pair, including indexes, gold, bitcoins, etc.
                            if (position.StopLoss == null || newStopLossPrice < position.StopLoss)
                            {

                                ModifyPosition(position, newStopLossPrice, position.TakeProfit);

                            }
                        }
                    }

                    //else meaning TradeType.Buy
                    else
                    {

                        double distance = Symbol.Bid - position.EntryPrice;

                        if (distance >= Trigger * Symbol.PipSize)
                        {

                            double newStopLossPrice = Symbol.Bid - Trailing * Symbol.PipSize;

                            if (position.StopLoss == null || newStopLossPrice > position.StopLoss)
                            {

                                ModifyPosition(position, newStopLossPrice, position.TakeProfit);

                            }
                        }
                    }
                }
            }
        }

        //protected override void OnStop()
        //{
        // Put your deinitialization logic here
        //}
    }
}
