using System;
using cAlgo.API;
using cAlgo.API.Internals;

namespace cAlgo.Robots.JFL_v27_Polytrend
{
    public class LineTouchDetector
    {
        private readonly Symbol _symbol;
        private readonly double _offsetPips;

        public LineTouchDetector(Symbol symbol, double offsetPips = 0.5)
        {
            _symbol = symbol;
            _offsetPips = offsetPips;
        }

        // Returns true if the last bar AFTER the pivot that crosses the line does so only via wick (tested = Dashed).
        // Returns false if the body crosses (untested = Dotted) or no bar after the pivot crosses at all.
        public bool IsWickTested(double linePrice, Bars bars, DateTime pivotTime = default)
        {
            if (bars == null || bars.Count < 2) return false;

            double offset = _offsetPips * _symbol.PipSize;
            int startBar = bars.Count - 2; // last closed bar
            int pivotBar = pivotTime == default
                ? Math.Max(0, bars.Count - 500)
                : Math.Max(0, bars.OpenTimes.GetIndexByTime(pivotTime));

            // Only inspect bars strictly AFTER the pivot (exclude the pivot itself)
            for (int i = startBar; i > pivotBar; i--)
            {
                double high = bars.HighPrices[i];
                double low  = bars.LowPrices[i];

                bool crosses = low <= linePrice + offset && high >= linePrice - offset;
                if (!crosses) continue;

                // Found the most-recent bar that crosses the line
                double bodyTop    = Math.Max(bars.OpenPrices[i], bars.ClosePrices[i]);
                double bodyBottom = Math.Min(bars.OpenPrices[i], bars.ClosePrices[i]);

                bool bodyTouches = linePrice >= bodyBottom && linePrice <= bodyTop;
                return !bodyTouches; // wick-only → tested (Dashed); body → untested (Dotted)
            }

            return false; // no bar after the pivot crosses the line
        }
    }
}
