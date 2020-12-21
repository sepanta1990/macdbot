using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using System.Threading.Tasks;
using System.Net;
using System.Collections.Generic;
using System.Timers;
using System.Web.Script.Serialization;
using Newtonsoft.Json;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class MACDBOT : Robot
    {
        string message = "";

        protected override void OnStart()
        {
            Timer.Start(120);

            OnTimedEvent();

        }

        protected override void OnTimer()
        {
            OnTimedEvent();
        }

        private void OnTimedEvent()
        {

            int minute = Server.Time.Minute;
            Print("Server.Time.Date.Minute {0}", minute);


            if (minute <= 0 || minute > 6)
            {
                return;
            }

            foreach (var p in Positions)
            {
                var warn = checkForPullBack(p);
                if (warn != null)
                {
                    sendMessageToTelegram(warn);
                }
            }


            foreach (var symboName in Symbols)
            {
                processSymbol(symboName);
            }

            if (!string.IsNullOrEmpty(message))
            {
                var result = sendMessageToTelegram(message);
                message = "";
            }

        }


        private void processSymbol(string symbolName)
        {
            Print("Processing symbol: {0}", symbolName);
            var symbolSeries = MarketData.GetBars(TimeFrame.Hour, symbolName).OpenPrices;


            var signals = Indicators.MacdCrossOver(symbolSeries, 26, 12, 9).Signal;
            var macd = Indicators.MacdCrossOver(symbolSeries, 26, 12, 9).MACD;

            var macdHasCrossedAboveSignal = macd.HasCrossedAbove(signals, 1);
            var macdHasCrossedBelowSignal = macd.HasCrossedBelow(signals, 1);


            var ma50 = Indicators.SimpleMovingAverage(symbolSeries, 50).Result;
            var ma100 = Indicators.SimpleMovingAverage(symbolSeries, 100).Result;

            var isBearish = false;
            var isBullish = false;

            if (ma50.Last(0) > ma100.Last(0) && ma50.Last(1) > ma100.Last(1))
            {
                isBullish = true;
            }
            if (ma50.Last(0) < ma100.Last(0) && ma50.Last(1) < ma100.Last(1))
            {
                isBearish = true;
            }

            Print("Bearish: {0}, Bullish: {1}, CrossedAboveSignal: {2},CrossedBelowSignal: {3} {4}", isBearish, isBullish, macdHasCrossedAboveSignal, macdHasCrossedBelowSignal, symbolName);

            if (isBearish && macdHasCrossedBelowSignal)
            {
                message += symbolName + " : " + "SELL" + "\n\n";
            }
            if (isBullish && macdHasCrossedAboveSignal)
            {
                message += symbolName + " : " + "BUY" + "\n\n";
            }
        }

        private string checkForPullBack(Position pos)
        {
            Print("Checking pull back for symbol: {0}", pos.SymbolName);
            var symbolSeries = MarketData.GetBars(TimeFrame.Hour, pos.SymbolName).OpenPrices;


            var signals = Indicators.MacdCrossOver(symbolSeries, 26, 12, 9).Signal;
            var macd = Indicators.MacdCrossOver(symbolSeries, 26, 12, 9).MACD;

            var macdHasCrossedAboveSignal = macd.HasCrossedAbove(signals, 0);
            var macdHasCrossedBelowSignal = macd.HasCrossedBelow(signals, 0);

            if (pos.TradeType == TradeType.Buy && macdHasCrossedBelowSignal)
            {
                return "WARNING!!";
            }
            if (pos.TradeType == TradeType.Sell && macdHasCrossedAboveSignal)
            {
                return "WARNING!!";
            }

            return null;
        }


        private bool sendMessageToTelegram(string message)
        {
            try
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                using (WebClient wc = new WebClient())
                {

                    wc.Headers.Add("Content-Type", "application/json");
                    wc.Headers[HttpRequestHeader.Accept] = "application/json";

                    JavaScriptSerializer serializer = new JavaScriptSerializer();


                    SendMessageRequest req = new SendMessageRequest();
                    req.chat_id = "192851624";
                    req.text = message;

                    var jsonString = serializer.Serialize(req);


                    wc.UploadString(" https://api.telegram.org/bot1483742351:AAFaWO93C7g7-hjgir2nEWZ5IcOyoDAUaxE/sendMessage", jsonString);

                    return true;

                }

            } catch (Exception e)
            {
                Print("Exception: {0}", e.Message);
                return false;
            }
        }


    }
}


class SendMessageRequest
{
    [JsonProperty(PropertyName = "chat_id")]
    public string chat_id { get; set; }
    public string text { get; set; }
}
