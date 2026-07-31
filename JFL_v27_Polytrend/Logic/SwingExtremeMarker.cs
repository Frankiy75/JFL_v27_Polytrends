using System;
using cAlgo.API;

namespace cAlgo.Robots.JFL_v27_Polytrend
{
    public class SwingExtremeMarker
    {
        private readonly Chart _chart;
        private readonly Bars  _bars;
        private readonly int   _lookback;
        private readonly bool  _screenMode;
        private readonly Color _highColor;
        private readonly Color _lowColor;

        private const string HighName = "PT_SwingHigh";
        private const string LowName  = "PT_SwingLow";

        private DateTime _lastHighTime;
        private DateTime _lastLowTime;
        private int _lastFirstVisible = -1;
        private int _lastLastVisible  = -1;

        public SwingExtremeMarker(Chart chart, Bars bars, int lookback, bool screenMode,
            Color highColor, Color lowColor)
        {
            _chart      = chart;
            _bars       = bars;
            _lookback   = lookback;
            _screenMode = screenMode;
            _highColor  = highColor;
            _lowColor   = lowColor;
        }

        public void Update()
        {
            int start, end;

            if (_screenMode)
            {
                start = Math.Max(0, _chart.FirstVisibleBarIndex);
                end   = Math.Min(_bars.Count - 2, _chart.LastVisibleBarIndex);

                // If the visible range itself changed, force redraw even if the extreme bar is the same
                if (start != _lastFirstVisible || end != _lastLastVisible)
                {
                    _lastFirstVisible = start;
                    _lastLastVisible  = end;
                    _lastHighTime = default;
                    _lastLowTime  = default;
                }
            }
            else
            {
                end   = _bars.Count - 2;
                start = Math.Max(0, end - _lookback + 1);
            }

            if (end < start) return;

            double highPrice = double.MinValue;
            double lowPrice  = double.MaxValue;
            DateTime highTime = _bars.OpenTimes[start];
            DateTime lowTime  = _bars.OpenTimes[start];

            for (int i = start; i <= end; i++)
            {
                if (_bars.HighPrices[i] > highPrice) { highPrice = _bars.HighPrices[i]; highTime = _bars.OpenTimes[i]; }
                if (_bars.LowPrices[i]  < lowPrice)  { lowPrice  = _bars.LowPrices[i];  lowTime  = _bars.OpenTimes[i]; }
            }

            if (highTime != _lastHighTime)
            {
                _chart.RemoveObject(HighName);
                var hi = _chart.DrawIcon(HighName, ChartIconType.DownTriangle, highTime, highPrice, _highColor);
                if (hi != null) hi.IsInteractive = false;
                _lastHighTime = highTime;
            }

            if (lowTime != _lastLowTime)
            {
                _chart.RemoveObject(LowName);
                var lo = _chart.DrawIcon(LowName, ChartIconType.UpTriangle, lowTime, lowPrice, _lowColor);
                if (lo != null) lo.IsInteractive = false;
                _lastLowTime = lowTime;
            }
        }

        public void Clear()
        {
            _chart?.RemoveObject(HighName);
            _chart?.RemoveObject(LowName);
            _lastHighTime    = default;
            _lastLowTime     = default;
            _lastFirstVisible = -1;
            _lastLastVisible  = -1;
        }
    }
}
