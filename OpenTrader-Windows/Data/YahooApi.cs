using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Web;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
#if __WINDOWS__
using System.Windows;
#endif
#if __IOS__
using UIKit;
#endif
#if __MACOS__
using AppKit;
#endif

namespace OpenTrader.Data
{
    public class YahooApi : ExternalReader
    {
		override public string Name
        {
            get { return "YahooApi"; }
        }

        override public bool CanReadCurrent
        {
            get { return true; }
        }

        override public bool CanReadHistorical
        {
            get { return false; }
        }
		
		override public void ReadCurrent(DataFile dataFile)
		{
            string yahooURL = @"https://yfapi.net/v6/finance/quote?region=US&lang=en&symbols=" + dataFile.YahooCode;

            var yahooRequest = WebRequest.Create(yahooURL);
            yahooRequest.Method = "GET";
            yahooRequest.Headers.Add("accept: application/json");
            yahooRequest.Headers.Add("x-api-key: 9vjzvJbrZS2ht2hnfIOOq65fzUBJ9Ahe1NXLtlrM");
            var yahooResponse = yahooRequest.GetResponse();

            string json = "";
            using (var yahooStream = yahooResponse.GetResponseStream())
            {
                using (var yahooReader = new StreamReader(yahooStream))
                {
                    json = yahooReader.ReadToEnd();
                }
            }

            if (string.IsNullOrEmpty(json))
                return;

            var jParent = JObject.Parse(json);
            var quoteResponse = jParent["quoteResponse"];
            var result = quoteResponse["result"];
            foreach (var child in result.Children())
            {
                DateTime epoch = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

                long regularMarketTime = long.Parse(child["regularMarketTime"].ToString());

                var date = epoch.AddSeconds(regularMarketTime).ToLocalTime();

                var symbol = child["symbol"].ToString();
                var close = double.Parse(child["regularMarketPrice"].ToString());
                var high = double.Parse(child["regularMarketDayHigh"].ToString());
                var low = double.Parse(child["regularMarketDayLow"].ToString());
                var volume = double.Parse(child["regularMarketVolume"].ToString());
                var open = double.Parse(child["regularMarketOpen"].ToString());

                if (dataFile.YahooCode == symbol)
                {
                    AddBar(dataFile, date, open, high, low, close, volume, true);
                }
            }
        }


        public void ReadCurrent(DataSet dataSet)
        {

            if (dataSet == null || dataSet.DataFiles == null || dataSet.DataFiles.Count == 0)
                return;

            List<DataFile> dataFiles = new List<DataFile>(); // for the compiler

            for (int i = 0; i < dataSet.DataFiles.Count; i++)
            {
                var count = 0;
                string symbols = "";
                for(int j=0; j<10 && j+i< dataSet.DataFiles.Count; j++)
                {
                    if (count == 0)
                    {
                        dataFiles = new List<DataFile>();
                        dataFiles.Add(dataSet.DataFiles[i + j]);
                        symbols = dataSet.DataFiles[i+j].YahooCode;
                        count = 1;
                        i++;
                    }
                    else
                    {
                        dataFiles.Add(dataSet.DataFiles[i + j]);
                        symbols += "," + dataSet.DataFiles[i+j].YahooCode;
                        count++;
                        i++;
                    }
                }

                string yahooURL = @"https://yfapi.net/v6/finance/quote?region=US&lang=en&symbols=" + symbols.Replace(",","%2C");

                var yahooRequest = WebRequest.Create(yahooURL);
                yahooRequest.Method = "GET";
                yahooRequest.Headers.Add("accept: application/json");
                yahooRequest.Headers.Add("x-api-key: 9vjzvJbrZS2ht2hnfIOOq65fzUBJ9Ahe1NXLtlrM");
                var yahooResponse = yahooRequest.GetResponse();

                string json = "";
                using (var yahooStream = yahooResponse.GetResponseStream())
                {
                    using (var yahooReader = new StreamReader(yahooStream))
                    {
                        json = yahooReader.ReadToEnd();
                    }
                }

                if (string.IsNullOrEmpty(json))
                    return;

                var jParent = JObject.Parse(json);
                var quoteResponse = jParent["quoteResponse"];
                var result = quoteResponse["result"];
                foreach( var child in result.Children() )
                {
                    DateTime epoch = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

                    long regularMarketTime;

                    try
                    {
                        regularMarketTime = long.Parse(child["regularMarketTime"].ToString());
                    }
                    catch
                    {
                        continue;
                    }

                    var date = epoch.AddSeconds(regularMarketTime).ToLocalTime();

                    var symbol = child["symbol"].ToString();
                    var close = double.Parse(child["regularMarketPrice"].ToString());
                    var high = double.Parse(child["regularMarketDayHigh"].ToString());
                    var low = double.Parse(child["regularMarketDayLow"].ToString());
                    var volume = double.Parse(child["regularMarketVolume"].ToString());
                    var open = double.Parse(child["regularMarketOpen"].ToString());

                    var dataFile = dataFiles.Find(df => df.YahooCode.ToUpper() == symbol.ToUpper());
                    if (dataFile != null)
                    {
                        AddBar(dataFile, date, open, high, low, close, volume, true);
                    }
                }
            }
        }
    }
}