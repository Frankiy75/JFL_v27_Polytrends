using System;

namespace cAlgo.Robots.JFL_v27_Polytrend
{
    // =========================================================================================
    // PIVOT CLASS
    // =========================================================================================
    public class JFL_Pivot
    {
        public double Price { get; set; }
        public DateTime Time { get; set; }
        public bool IsHigh { get; set; }
        public bool IsConfirmed { get; set; }
        public double ClosePrice { get; set; }
        public double OpenPrice { get; set; }

        // BodyTop = max(Open,Close), BodyBottom = min(Open,Close)
        public double BodyTop    => Math.Max(OpenPrice, ClosePrice);
        public double BodyBottom => Math.Min(OpenPrice, ClosePrice);

        public JFL_Pivot(double price, DateTime time, bool isHigh, double closePrice, double openPrice = 0, bool confirmed = true)
        {
            Price = price;
            Time = time;
            IsHigh = isHigh;
            ClosePrice = closePrice;
            OpenPrice = openPrice;
            IsConfirmed = confirmed;
        }
    }
}
