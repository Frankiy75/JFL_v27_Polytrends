using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;

namespace cAlgo.Robots.JFL_v27_Polytrend
{
    // =========================================================================================
    // SERVICIO ZIGZAG - User Provided Implementation (JFL_26.cv)
    // =========================================================================================
    public class JFL_ZigZagService
    {
        private readonly Bars _bars;
        private readonly List<JFL_Pivot> _pivots;
        
        private double _currentExtreme;
        private DateTime _currentExtremeTime;
        private double _currentExtremeClose;
        private double _currentExtremeOpen;
        private string _currentDirection; // "up" or "down"

        // ✅ O(1) PERFORMANCE ROLLBACK: Primitive backup state
        private bool _hasCleanState;
        private int _cleanPivotCount;
        private double _cleanCurrentExtreme;
        private DateTime _cleanCurrentExtremeTime;
        private double _cleanCurrentExtremeClose;
        private double _cleanCurrentExtremeOpen;
        private string _cleanCurrentDirection;
        
        // State tracking to avoid unnecessary recalculations
        private int _lastCalculatedBarCount;
        private DateTime _lastCalculationTime;
        
        // ✅ TICK OPTIMIZATION: Cache to avoid redundant CPU usage
        private double _cachedAvgRange;
        private int _cachedAvgRangeBarCount;
        private double _lastCalcH0;
        private double _lastCalcL0;
        private double _lastCalcC0;
        
        public JFL_ZigZagService(Bars bars)
        {
            _bars = bars;
            _pivots = new List<JFL_Pivot>();
            _currentDirection = "up";
            _currentExtreme = 0.0;
            _lastCalculatedBarCount = 0;
            _lastCalculationTime = DateTime.MinValue;
            _hasCleanState = false;
        }

        public List<JFL_Pivot> Pivots => _pivots;
        public int PivotCount => _pivots.Count;
        public TimeFrame TimeFrame => _bars.TimeFrame;
        public string Direction => _currentDirection;
        
        // For trading logic: 1 = up, -1 = down, 0 = unknown
        public int CurrentDirection
        {
            get
            {
                if (_currentDirection == "up") return 1;
                if (_currentDirection == "down") return -1;
                return 0;
            }
        }

        public void Calculate()
        {
            int totalBars = _bars.Count;
            
            // ✅ OPTIMIZATION: Data Validation - Prevent crashes from bad data
            if (totalBars < 2) return;
            
            // Validate bar data integrity
            if (_bars.HighPrices == null || _bars.LowPrices == null || 
                _bars.OpenPrices == null || _bars.ClosePrices == null || 
                _bars.OpenTimes == null)
            {
                return; // Silently skip if data is invalid
            }

            // ✅ Only recalculate if bars changed (removed time throttle for real-time updates)
            bool barsChanged = totalBars != _lastCalculatedBarCount;
            
            double h0 = _bars.HighPrices.Last(0);
            double l0 = _bars.LowPrices.Last(0);
            double c0 = _bars.ClosePrices.Last(0);

            // Always update on bar change, or process bar 0 if state exists
            if (!barsChanged && !_hasCleanState)
                return;
                
            // ✅ OPTIMIZATION: Skip processing if current tick didn't change H, L, or C
            if (!barsChanged && _hasCleanState)
            {
                if (h0 == _lastCalcH0 && l0 == _lastCalcL0 && c0 == _lastCalcC0)
                    return;
            }
            
            _lastCalcH0 = h0;
            _lastCalcL0 = l0;
            _lastCalcC0 = c0;
            
            // ✅ MQL5-STYLE: Detect if this is first calculation or bar change
            int delta = Math.Abs(totalBars - _lastCalculatedBarCount);
            // Only reset on first run or when bars DECREASED (history trimmed), never on burst of new bars
            bool isHistoryChange = _lastCalculatedBarCount > 0 && totalBars < _lastCalculatedBarCount;

            bool isFirstRun = (!_hasCleanState || _pivots.Count == 0 || isHistoryChange);
            // Cap incremental processing at 50 bars; beyond that do a full recalc
            bool newBarClosed = !isFirstRun && barsChanged && _lastCalculatedBarCount > 0 && delta <= 50;
            
            // FULL RECALCULATION (first run only)
            if (isFirstRun)
            {
                _pivots.Clear();
                _currentDirection = "up";
                _currentExtreme = 0.0;
                _currentExtremeTime = DateTime.MinValue;
                
                // ✅ OPTIMIZATION: Limited lookback to 1000 bars for better startup performance
                int lookback = 1000;
                int startIndex = Math.Max(0, totalBars - lookback);

                // Calculate average range
            double avgRange = 0;
            int rangeCount = Math.Min(10, totalBars);
            for (int shift = 0; shift < rangeCount; shift++)
            {
                avgRange += (_bars.HighPrices.Last(shift) - _bars.LowPrices.Last(shift));
            }
            avgRange /= rangeCount > 0 ? rangeCount : 1;

            int maxShift = totalBars - 1 - startIndex;
            
            // Process historical bars (skip bar 0)
            for (int shift = maxShift; shift >= 1; shift--)
            {
                // Current bar
                double h = _bars.HighPrices.Last(shift);
                double l = _bars.LowPrices.Last(shift);
                DateTime t = _bars.OpenTimes.Last(shift);
                double c = _bars.ClosePrices.Last(shift);
                double o = _bars.OpenPrices.Last(shift);
                double currentBarRange = h - l;

                // Previous bar for comparison
                double prevH = _bars.HighPrices.Last(shift + 1);
                double prevL = _bars.LowPrices.Last(shift + 1);

                if (h > prevH && l < prevL)
                {
                    bool bullish = c > o;
                    if (bullish)
                    {
                        Scan(l, false, t, avgRange, currentBarRange, c, o);
                        Scan(h, true,  t, avgRange, currentBarRange, c, o);
                    }
                    else
                    {
                        Scan(h, true,  t, avgRange, currentBarRange, c, o);
                        Scan(l, false, t, avgRange, currentBarRange, c, o);
                    }
                }
                else
                {
                    if (h > prevH) Scan(h, true,  t, avgRange, currentBarRange, c, o);
                    if (l < prevL) Scan(l, false, t, avgRange, currentBarRange, c, o);
                }
            }

                // ✅ SAVE CLEAN STATE after first full calculation
                SaveState();
                _lastCalculatedBarCount = totalBars;
            }
            // NEW BAR CLOSED: Process only the new bar(s)
            else if (newBarClosed)
            {
                // Restore clean state first
                RestoreState();
                
                // Calculate average range
                double avgRange = 0;
                int rangeCount = Math.Min(10, totalBars);
                for (int shift = 0; shift < rangeCount; shift++)
                {
                    avgRange += (_bars.HighPrices.Last(shift) - _bars.LowPrices.Last(shift));
                }
                avgRange /= rangeCount > 0 ? rangeCount : 1;
                
                // Process only the newly closed bar(s)
                int newBars = totalBars - _lastCalculatedBarCount;
                for (int shift = newBars; shift >= 1; shift--)
                {
                    double h = _bars.HighPrices.Last(shift);
                    double l = _bars.LowPrices.Last(shift);
                    DateTime t = _bars.OpenTimes.Last(shift);
                    double c = _bars.ClosePrices.Last(shift);
                    double o = _bars.OpenPrices.Last(shift);
                    double currentBarRange = h - l;

                    double prevH = _bars.HighPrices.Last(shift + 1);
                    double prevL = _bars.LowPrices.Last(shift + 1);

                    if (h > prevH && l < prevL)
                    {
                        bool bullish = c > o;
                        if (bullish)
                        {
                            Scan(l, false, t, avgRange, currentBarRange, c, o);
                            Scan(h, true,  t, avgRange, currentBarRange, c, o);
                        }
                        else
                        {
                            Scan(h, true,  t, avgRange, currentBarRange, c, o);
                            Scan(l, false, t, avgRange, currentBarRange, c, o);
                        }
                    }
                    else
                    {
                        if (h > prevH) Scan(h, true,  t, avgRange, currentBarRange, c, o);
                        if (l < prevL) Scan(l, false, t, avgRange, currentBarRange, c, o);
                    }
                }
                
                // ✅ SAVE NEW CLEAN STATE
                SaveState();
                _lastCalculatedBarCount = totalBars;
            }
            else
            {
                // SAME BAR (tick update): Restore clean state
                RestoreState();
            }
            
            // ✅ MQL5-STYLE: Always process bar 0 (current bar) to update current extreme
            // Calculate average range for bar 0 (Cached to avoid loop on every tick)
            double avgRange0 = 0;
            if (_cachedAvgRangeBarCount == totalBars)
            {
                avgRange0 = _cachedAvgRange;
            }
            else
            {
                int rangeCount0 = Math.Min(10, totalBars);
                for (int shift = 1; shift < rangeCount0; shift++) // Start at 1 to skip bar 0
                {
                    avgRange0 += (_bars.HighPrices.Last(shift) - _bars.LowPrices.Last(shift));
                }
                avgRange0 /= (rangeCount0 - 1) > 0 ? (rangeCount0 - 1) : 1;
                _cachedAvgRange = avgRange0;
                _cachedAvgRangeBarCount = totalBars;
            }
            
            DateTime t0 = _bars.OpenTimes.Last(0);
            double o0 = _bars.OpenPrices.Last(0);
            double range0 = h0 - l0;
            
            if (_bars.Count > 1)
            {
                double prevH = _bars.HighPrices.Last(1);
                double prevL = _bars.LowPrices.Last(1);
                
                if (h0 > prevH && l0 < prevL)
                {
                    bool bullish = c0 > o0;
                    if (bullish)
                    {
                        Scan(l0, false, t0, avgRange0, range0, c0, o0);
                        Scan(h0, true,  t0, avgRange0, range0, c0, o0);
                    }
                    else
                    {
                        Scan(h0, true,  t0, avgRange0, range0, c0, o0);
                        Scan(l0, false, t0, avgRange0, range0, c0, o0);
                    }
                }
                else
                {
                    if (h0 > prevH) Scan(h0, true,  t0, avgRange0, range0, c0, o0);
                    if (l0 < prevL) Scan(l0, false, t0, avgRange0, range0, c0, o0);
                }
            }
            
            // ✅ CRITICAL FIX: Always update extreme to current bar's extreme if it's better
            // This ensures the last segment always reaches the current price
            // Use >= and <= to always update even if equal (covers same tick updates)
            if (_currentDirection == "up")
            {
                if (h0 >= _currentExtreme)
                {
                    _currentExtreme = h0;
                    _currentExtremeTime = t0;
                    _currentExtremeClose = c0;
                    _currentExtremeOpen = o0;
                }
            }
            else
            {
                if (l0 <= _currentExtreme || _currentExtreme == 0)
                {
                    _currentExtreme = l0;
                    _currentExtremeTime = t0;
                    _currentExtremeClose = c0;
                    _currentExtremeOpen = o0;
                }
            }
        }

        private void Scan(double price, bool isHigh, DateTime time, double avgRange, double currentBarRange, double closePrice, double openPrice)
        {
            if (_currentExtreme == 0.0)
            {
                _currentExtreme = price;
                _currentExtremeTime = time;
                _currentExtremeClose = closePrice;
                _currentExtremeOpen = openPrice;
                _currentDirection = isHigh ? "down" : "up";
                return;
            }

            if ((isHigh && _currentDirection == "up" && price > _currentExtreme) ||
                (!isHigh && _currentDirection == "down" && price < _currentExtreme))
            {
                _currentExtreme = price;
                _currentExtremeTime = time;
                _currentExtremeClose = closePrice;
                _currentExtremeOpen = openPrice;
                return;
            }

            if ((isHigh && _currentDirection == "down") || (!isHigh && _currentDirection == "up"))
            {
                double movement = Math.Abs(price - _currentExtreme);
                double threshold = Math.Max(avgRange * 0.05, currentBarRange * 0.10);

                if (movement < threshold)
                {
                    bool isBetter = (isHigh && price > _currentExtreme) || (!isHigh && price < _currentExtreme);
                    if (isBetter)
                    {
                        _currentExtreme = price;
                        _currentExtremeTime = time;
                        _currentExtremeClose = closePrice;
                        _currentExtremeOpen = openPrice;
                    }
                    return;
                }

                AddPivot(_currentExtreme, _currentExtremeTime, _currentDirection == "up", _currentExtremeClose, _currentExtremeOpen);

                _currentDirection = isHigh ? "up" : "down";
                _currentExtreme = price;
                _currentExtremeTime = time;
                _currentExtremeClose = closePrice;
                _currentExtremeOpen = openPrice;
            }
        }

        private void AddPivot(double price, DateTime time, bool isHigh, double closePrice, double openPrice = 0)
        {
            // ✅ SAFEGUARD: Prevent duplicate pivots of same type on same bar (fixes double line issue)
            if (_pivots.Count > 0)
            {
                var last = _pivots.Last();
                
                // Check if on same bar
                TimeSpan barDuration = GetTimeFrameSpan(_bars.TimeFrame);
                bool sameBar = (time - last.Time) < barDuration;
                
                if (sameBar && last.IsHigh == isHigh)
                {
                    bool isBetter = (isHigh && price > last.Price) || (!isHigh && price < last.Price);
                    if (isBetter)
                    {
                        last.Price = price;
                        last.Time = time;
                        last.ClosePrice = closePrice;
                        last.OpenPrice = openPrice;
                    }
                    return;
                }

                // Same type on different bar: only replace if it's a strict improvement (never erase valid pivot)
                if (last.IsHigh == isHigh)
                {
                    bool isBetter = (isHigh && price > last.Price) || (!isHigh && price < last.Price);
                    if (isBetter)
                    {
                        last.Price = price;
                        last.Time = time;
                        last.ClosePrice = closePrice;
                        last.OpenPrice = openPrice;
                    }
                    return; // Same type → never add a second consecutive pivot of same direction
                }
            }

            _pivots.Add(new JFL_Pivot(price, time, isHigh, closePrice, openPrice));

            // Limit history
            if (_pivots.Count > 1000)
            {
                _pivots.RemoveAt(0);
                if (_hasCleanState && _cleanPivotCount > 0)
                {
                    _cleanPivotCount--;
                }
            }
        }

        public JFL_Pivot GetCurrentExtreme()
        {
            if (_currentExtreme == 0.0) return null;
            return new JFL_Pivot(_currentExtreme, _currentExtremeTime, _currentDirection == "up", _currentExtremeClose, _currentExtremeOpen, false);
        }
        
        // ✅ MQL5-STYLE: Save current state (after processing closed bars)
        private void SaveState()
        {
            _hasCleanState = true;
            _cleanPivotCount = _pivots.Count;
            _cleanCurrentExtreme = _currentExtreme;
            _cleanCurrentExtremeTime = _currentExtremeTime;
            _cleanCurrentExtremeClose = _currentExtremeClose;
            _cleanCurrentExtremeOpen = _currentExtremeOpen;
            _cleanCurrentDirection = _currentDirection;
        }

        private void RestoreState()
        {
            if (!_hasCleanState) return;

            if (_pivots.Count > _cleanPivotCount)
                _pivots.RemoveRange(_cleanPivotCount, _pivots.Count - _cleanPivotCount);

            _currentExtreme = _cleanCurrentExtreme;
            _currentExtremeTime = _cleanCurrentExtremeTime;
            _currentExtremeClose = _cleanCurrentExtremeClose;
            _currentExtremeOpen = _cleanCurrentExtremeOpen;
            _currentDirection = _cleanCurrentDirection;
        }

        // ✅ FLICKER-FIX: Restore pivots from static cache
        public void RestorePivots(List<JFL_Pivot> cachedPivots)
        {
            if (cachedPivots == null || cachedPivots.Count == 0) return;
            
            _pivots.Clear();
            _pivots.AddRange(cachedPivots);
            
            // Set initial state based on last pivot to allow continuation
            var last = _pivots.Last();
            _currentExtreme = last.Price;
            _currentExtremeTime = last.Time;
            _currentExtremeClose = last.ClosePrice;
            _currentExtremeOpen = last.OpenPrice;
            _currentDirection = last.IsHigh ? "down" : "up";
            
            SaveState(); // Make it the clean state for bar 0 processing
        }
        
        public double GetZigZagMinimum() 
        { 
             if (_pivots.Count == 0) return 0;
             return _pivots.Where(p => !p.IsHigh).Min(p => p.Price);
        }
        
        public double GetZigZagMaximum() 
        { 
             if (_pivots.Count == 0) return 0;
             return _pivots.Where(p => p.IsHigh).Max(p => p.Price);
        }
        
        private TimeSpan GetTimeFrameSpan(TimeFrame tf)
        {
            if (tf == TimeFrame.Minute)   return TimeSpan.FromMinutes(1);
            if (tf == TimeFrame.Minute3)  return TimeSpan.FromMinutes(3);
            if (tf == TimeFrame.Minute5)  return TimeSpan.FromMinutes(5);
            if (tf == TimeFrame.Minute15) return TimeSpan.FromMinutes(15);
            if (tf == TimeFrame.Hour)     return TimeSpan.FromHours(1);
            if (tf == TimeFrame.Hour4)    return TimeSpan.FromHours(4);
            if (tf == TimeFrame.Hour12)   return TimeSpan.FromHours(12);
            if (tf == TimeFrame.Daily)    return TimeSpan.FromDays(1);
            if (tf == TimeFrame.Weekly)   return TimeSpan.FromDays(7);
            if (tf == TimeFrame.Monthly)  return TimeSpan.FromDays(30);
            
            return TimeSpan.FromHours(1); 
        }
    }
}
