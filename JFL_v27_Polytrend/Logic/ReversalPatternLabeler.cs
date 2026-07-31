using System;
using System.Collections.Generic;
using cAlgo.API;

namespace cAlgo.Robots.JFL_v27_Polytrend
{
    public class ReversalPatternLabeler
    {
        private readonly Chart _chart;
        private readonly Bars _bars;
        private readonly string _labelPrefix;
        private readonly string _tfLabel;
        private readonly Color _highColor;
        private readonly Color _lowColor;
        private readonly int _fontSize;
        private readonly bool _useCandleBodyFilter;

        private readonly HashSet<DateTime> _peaksCache   = new HashSet<DateTime>();
        private readonly HashSet<DateTime> _valleysCache = new HashSet<DateTime>();
        private readonly HashSet<string>   _drawnLabels  = new HashSet<string>();

        public ReversalPatternLabeler(Chart chart, Bars bars,
            Color highColor, Color lowColor, int fontSize, bool useCandleBodyFilter = false)
        {
            _chart               = chart;
            _bars                = bars;
            _highColor           = highColor;
            _lowColor            = lowColor;
            _fontSize            = fontSize;
            _useCandleBodyFilter = useCandleBodyFilter;
            _tfLabel             = TfUtils.GetLabel(bars.TimeFrame);
            _labelPrefix         = $"PT_RevLbl_{_tfLabel}_";

            Clear();
        }

        public void ScanAndLabel(int limit = 300)
        {
            CleanupInvalidated();

            int start = Math.Max(2, _bars.Count - limit);
            int end   = _bars.Count - 3;

            for (int i = start; i <= end; i++)
            {
                if (IsHighPeak(i))  DrawLabel(i, isHigh: true);
                if (IsLowValley(i)) DrawLabel(i, isHigh: false);
            }
        }

        public bool IsHighPeak(int i)
        {
            if (i < 2 || i >= _bars.Count - 2) return false;
            double h0  = _bars.HighPrices[i];
            double hm1 = _bars.HighPrices[i - 1];
            double hm2 = _bars.HighPrices[i - 2];
            double hp1 = _bars.HighPrices[i + 1];
            double hp2 = _bars.HighPrices[i + 2];
            if (!(hm2 < hm1 && hm1 <= h0 && h0 >= hp1 && hp1 > hp2 && hp1 != h0)) return false;
            if (_useCandleBodyFilter && !MatchesCandlePattern(i, isHigh: true)) return false;
            return true;
        }

        public bool IsLowValley(int i)
        {
            if (i < 2 || i >= _bars.Count - 2) return false;
            double l0  = _bars.LowPrices[i];
            double lm1 = _bars.LowPrices[i - 1];
            double lm2 = _bars.LowPrices[i - 2];
            double lp1 = _bars.LowPrices[i + 1];
            double lp2 = _bars.LowPrices[i + 2];
            if (!(lm2 > lm1 && lm1 >= l0 && l0 <= lp1 && lp1 < lp2 && lp1 != l0)) return false;
            if (_useCandleBodyFilter && !MatchesCandlePattern(i, isHigh: false)) return false;
            return true;
        }

        private void CleanupInvalidated()
        {
            var remove = new List<DateTime>();
            foreach (var t in _peaksCache)
            {
                int idx = _bars.OpenTimes.GetIndexByTime(t);
                if (idx >= 2 && idx < _bars.Count - 2 && !IsHighPeak(idx)) remove.Add(t);
            }
            foreach (var t in remove) RemoveLabel(t, _peaksCache);

            remove.Clear();
            foreach (var t in _valleysCache)
            {
                int idx = _bars.OpenTimes.GetIndexByTime(t);
                if (idx >= 2 && idx < _bars.Count - 2 && !IsLowValley(idx)) remove.Add(t);
            }
            foreach (var t in remove) RemoveLabel(t, _valleysCache);
        }

        private void RemoveLabel(DateTime t, HashSet<DateTime> cache)
        {
            cache.Remove(t);
            string name = $"{_labelPrefix}{t.Ticks}";
            _drawnLabels.Remove(name);
            _chart?.RemoveObject(name);
        }

        private void DrawLabel(int index, bool isHigh)
        {
            DateTime time = _bars.OpenTimes[index];
            string name   = $"{_labelPrefix}{time.Ticks}";

            if (isHigh) _peaksCache.Add(time);
            else        _valleysCache.Add(time);

            if (_chart == null || _drawnLabels.Contains(name)) return;

            double price = isHigh ? _bars.HighPrices[index] : _bars.LowPrices[index];
            Color  color = isHigh ? _highColor : _lowColor;
            var vAlign   = isHigh ? VerticalAlignment.Top : VerticalAlignment.Bottom;

            var text = _chart.DrawText(name, _tfLabel, time, price, color);
            if (text != null)
            {
                text.FontSize            = _fontSize;
                text.IsInteractive       = false;
                text.VerticalAlignment   = vAlign;
                text.HorizontalAlignment = HorizontalAlignment.Center;
            }

            _drawnLabels.Add(name);
        }

        public void Clear()
        {
            _peaksCache.Clear();
            _valleysCache.Clear();
            _drawnLabels.Clear();

            if (_chart == null) return;
            for (int i = _chart.Objects.Count - 1; i >= 0; i--)
            {
                var obj = _chart.Objects[i];
                if (obj != null && obj.Name != null && obj.Name.StartsWith(_labelPrefix))
                    _chart.RemoveObject(obj.Name);
            }
        }

        private bool IsBull(int i) => _bars.ClosePrices[i] > _bars.OpenPrices[i];
        private bool IsBear(int i) => _bars.OpenPrices[i] > _bars.ClosePrices[i];

        // For peaks (isHigh=true): 2 bull on left, 2 bear on right (or shifted variants).
        // For valleys (isHigh=false): bull/bear swapped.
        private bool MatchesCandlePattern(int i, bool isHigh)
        {
            bool bull(int j) => isHigh ? IsBull(j) : IsBear(j);
            bool bear(int j) => isHigh ? IsBear(j) : IsBull(j);

            return (bull(i - 1) && bull(i) && bear(i + 1) && bear(i + 2)) ||
                   (bear(i - 2) && bear(i - 1) && bull(i) && bull(i + 1)) ||
                   (bull(i - 2) && bull(i - 1) && bear(i) && bear(i + 1)) ||
                   (bear(i - 1) && bear(i) && bull(i + 1) && bull(i + 2));
        }
    }
}
