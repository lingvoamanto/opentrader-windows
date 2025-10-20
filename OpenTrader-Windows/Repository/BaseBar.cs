using System;
namespace OpenTrader
{
    public interface IBar
    {
        #region properties
        public int Id { get; set; }
        public string YahooCode { get; set; }
        public DateTime Date { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public double Volume { get; set; }
        public bool Interim { get; set; }
        #endregion properties
    }
}
