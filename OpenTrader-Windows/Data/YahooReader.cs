using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Web;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;
using OpenCompiler;

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
    public class YahooReader : ExternalReader
    {
        override public string Name
        {
            get { return "Yahoo"; }
        }

        override public bool CanReadCurrent
        {
            get { return true; }
        }

        override public bool CanReadHistorical
        {
            get { return true; }
        }

        Random rnd = new Random();

        override public void ReadCurrent(DataFile datafile)
        {
            DataSet dataset = datafile.DataSet;
            Bars bars = datafile.bars;
            List<bool> interim = datafile.interim;
            List<int> barId = datafile.barId;
            string code = datafile.ReaderCodes[Name];
            if (code == "" || code == null)
            {
                code = (datafile.YahooCode == "" ? datafile.Name : datafile.YahooCode);
                code = dataset.YahooPrefix + code + dataset.YahooSuffix;
            }

            string YahooURL = @"https://au.finance.yahoo.com/d/quotes.csv?s=" + code + "&f=sl1d1t1c1ohgv&e=.csv";
            WebRequest YahooRequest = WebRequest.Create(YahooURL);
            WebResponse YahooResponse = YahooRequest.GetResponse();
            Stream YahooStream = YahooResponse.GetResponseStream();
            StreamReader YahooStreamReader = new StreamReader(YahooStream);

            string YahooLine = YahooStreamReader.ReadLine();

            char[] Comma = { ',' };


            double open, high, low, close, volume;
            DateTime date = DateTime.Now;

            if (YahooLine != null)
            {
                string[] stringdata = YahooLine.Replace("\"", "").Split(Comma);
                try
                {
                    close = double.Parse(stringdata[1]);
                    open = double.Parse(stringdata[5]);
                    high = double.Parse(stringdata[6]);
                    low = double.Parse(stringdata[7]);

                    char[] Slash = { '/' };
                    string[] stringDate = stringdata[2].Split(Slash);
                    int Year = Int32.Parse(stringDate[2]);
                    int Month = Int32.Parse(stringDate[0]);
                    int Day = Int32.Parse(stringDate[1]);
                    date = new DateTime(Year, Month, Day);

                    volume = double.Parse(stringdata[8]);
                }
                catch
                {
                    close = double.NaN;
                    open = double.NaN;
                    high = double.NaN;
                    low = double.NaN;
                    volume = double.NaN;
                }

                if (!double.IsNaN(close) && !double.IsNaN(volume) && volume > float.Epsilon)
                {
                    bool found = false;
                    int index = bars.Find(date, out found);
                    if (!found)
                    {
                        bars.Insert(index, date, open, high, low, close, volume);
                        interim.Insert(index, true);
                        barId.Insert(index, 0);
                    }
                    else
                    {
                        if (interim[index])
                        {
                            bars.Replace(index, date, open, high, low, close, volume);
                            interim[index] = false;
                        }
                    }
                }
            }
            YahooStreamReader.Close();
            YahooStream.Close();
            YahooResponse.Close();
        }

        static string _cookie = "d=AQABBBpLM2UCEI73gB0qfLO6__6KALoKQHEFEgEBCAGAU2iCaIrCNuUA_eMDAAcIGkszZboKQHE&S=AQAAAu1ZnisWf0JW-7Zza2qspaA";

        public ReadResult ReadHistorical(DataFile datafile, DateTime startDate, DateTime lastDate, bool ignoreErrors = false)
        {
            /*
            var problemDate = new DateTime(2024, 2, 1).ToUniversalTime().Date;
            if (startDate > problemDate)
            {
                startDate = problemDate;
            }
            */

            ReadResult result = ReadResult.Success;
            int index; // Here so we can debug
            DataSet dataset = datafile.DataSet;
            string yahoocode = datafile.ReaderCodes[Name];
            if (yahoocode == "" || yahoocode == null)
            {
                yahoocode = (datafile.YahooCode == "" ? datafile.Name : datafile.YahooCode);
                if (dataset != null)
                {
                    yahoocode = dataset.YahooPrefix + yahoocode + dataset.YahooSuffix;
                }
            }

            //          DateTime LastDate = Bars[0].Date.AddDays( -1 );
            string Y = lastDate.Year.ToString();
            string M = lastDate.Month.ToString();
            string D = lastDate.Day.ToString();

            // https://finance.yahoo.com/quote/AAPL/history/?period1=345479400&period2=1745890793

            string urlScrape = "https://finance.yahoo.com/quote/{0}/history/?period1={1}&period2={2}";
            DateTime epoch = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            double unixStart = (startDate.ToUniversalTime() - epoch).TotalSeconds;
            double unixFinish = (DateTime.Now.AddDays(2).ToUniversalTime() - epoch).TotalSeconds;

            var yahooURL = string.Format(urlScrape, HttpUtility.HtmlEncode(yahoocode), Math.Round(unixStart, 0), Math.Round(unixFinish, 0));


            var request = (HttpWebRequest)WebRequest.Create(yahooURL);
            request.Timeout = 180000;
            request.AllowAutoRedirect = true;
            var cookies = new CookieContainer();
            if (request.CookieContainer == null)
            {
                request.CookieContainer = new CookieContainer();
            }
            foreach(var cookie in Cookies)
            {
                request.CookieContainer.Add(cookie);
            }

            request.Method = "GET";
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls; //  | SecurityProtocolType.Ssl3;

            string[] agents =  { @"Mozilla/5.0 (Windows NT 6.2; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/32.0.1667.0 Safari/537.36",
                                   @"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:75.0) Gecko/20100101 Firefox/75.0",
                @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36 Edg/122.0.0.0" };
            agents = new string[] { "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36 Edg/137.0.0.0" };
            int agent = rnd.Next(0, agents.Length);

            request.UserAgent = agents[agent];
            request.Headers["User-Agent"] = agents[agent];
            var html = string.Empty;
            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    using (var stream = response.GetResponseStream())
                    {
                        if (stream != null)
                        {
                            html = new StreamReader(stream).ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return ReadResult.NoResponse;
            }

            if (html == string.Empty)
            {
                return ReadResult.Empty;
            }

            var tbodyStart = html.IndexOf(@"<tbody>");
            var tbodyEnd = html.IndexOf(@"</tbody>");

            if (tbodyStart == -1 || tbodyEnd == -1)
            {
                return ReadResult.NoTBody;
            }

            html = html.Substring(tbodyStart + 7, tbodyEnd - tbodyStart - 7);

            MatchCollection? matchedRows = null;
            try
            {
                Regex jsonRegex = new Regex(@"(<td\s*class=""[a-z1-9\-]*"">[a-zA-Z0-9\,\.\s]*<\/td>\s*){7}");
                matchedRows = jsonRegex.Matches(html);
            }
            catch (Exception ex)
            {
                return ReadResult.BadMatch;
            }

            foreach (var row in matchedRows)
            {
                var line = row.ToString();
                Regex lineRegex = new Regex(@"<td\s*class=""[a-z1-9\-]*"">([a-zA-Z0-9\,\.\s]*)<\/td>");
                var matchedCells = lineRegex.Matches(row.ToString());
                var dateString = matchedCells[0].Groups[1].ToString();
                var openString = matchedCells[1].Groups[1].ToString();
                var highString = matchedCells[2].Groups[1].ToString();
                var lowString = matchedCells[3].Groups[1].ToString();
                var closeString = matchedCells[4].Groups[1].ToString();
                var volumeString = matchedCells[6].Groups[1].ToString();

                DateTime date = DateTime.Parse(dateString);
                double open = double.Parse(openString);
                double high = double.Parse(highString);
                double low = double.Parse(lowString);
                double close = double.Parse(closeString);
                // adjusted volume is at 5
                double volume = double.Parse(volumeString);

                if (volume > float.Epsilon)
                {
                    AddBar(datafile, date, open, high, low, close, volume, true);
                }
                else
                {
                    bool found = false;
                    if (datafile.bars.Count == 0)
                        AddBar(datafile, date, open, high, low, close, volume, true);
                    else
                    {
                        index = datafile.bars.Find(date, out found);
                        if (index == -1 || datafile.bars.Volume[index] <= float.Epsilon)
                            AddBar(datafile, date, open, high, low, close, volume, true);
                    }
                }
            }
            return ReadResult.Success;
        }

        static public List<System.Net.Cookie> Cookies = new();
    }
}

