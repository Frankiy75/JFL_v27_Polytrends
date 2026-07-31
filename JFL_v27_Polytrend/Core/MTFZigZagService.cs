using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Internals;

namespace cAlgo.Robots.JFL_v27_Polytrend
{
    /// <summary>
    /// Manages ZigZag calculations for multiple timeframes.
    /// This service acts as a centralized provider for MTF data.
    /// </summary>
    public class MTFZigZagService
    {
        private readonly Robot _robot;
        private readonly MarketData _marketData;
        private readonly string _symbolName;
        private readonly bool _debugMode;
        private readonly Action<string> _print;
        
        // Dictionary to store ZigZag services for each timeframe
        private Dictionary<TimeFrame, JFL_ZigZagService> _zigzags;
        private Dictionary<TimeFrame, Bars> _bars;
        
        // Lazy Loading - Track bar counts to skip unchanged timeframes
        private Dictionary<TimeFrame, int> _lastBarCount;
        private DateTime _lastUpdate = DateTime.MinValue;
        private readonly object _syncLock = new object();
        private volatile bool _isDisconnected = false;
        private List<TimeFrame> _cachedActiveTimeFrames = new List<TimeFrame>();
        
        public bool IsReady { get; private set; }
        public string SymbolName => _symbolName;
        
        public event Action<TimeFrame> OnHistoryLoaded;
        
        public Robot BaseRobot => _robot;

        public MTFZigZagService(Robot robot, bool debugMode, Action<string> print)
        {
            _robot = robot;
            _marketData = robot.MarketData;
            _symbolName = robot.Symbol.Name;
            _debugMode = debugMode;
            _print = print;
            
            _zigzags = new Dictionary<TimeFrame, JFL_ZigZagService>();
            _bars = new Dictionary<TimeFrame, Bars>();
            _lastBarCount = new Dictionary<TimeFrame, int>();
            _historyHandlers = new Dictionary<TimeFrame, Action<BarsHistoryLoadedEventArgs>>();
            IsReady = false;
        }

        private Dictionary<TimeFrame, Action<BarsHistoryLoadedEventArgs>> _historyHandlers;
        private const int MTF_TARGET_BARS = 2000;
        
        public void InitializeTimeFrame(TimeFrame tf)
        {
            if (_zigzags.ContainsKey(tf))
                return;
                
            try
            {
                var bars = _marketData.GetBars(tf, _symbolName);
                if (bars == null)
                {
                    if (_debugMode) _print($"❌ MTF Init Error ({tf}): _marketData.GetBars returned null.");
                    return;
                }
                
                // Solo pedir más historia en TFs que pueden alcanzar el objetivo (no W/M)
                bool canReachTarget = tf != TimeFrame.Weekly && tf != TimeFrame.Monthly;
                if (canReachTarget && bars.Count < MTF_TARGET_BARS)
                {
                    if (_debugMode) _print($"🕒 MTF: TimeFrame {tf} loading background history... ({bars.Count}/{MTF_TARGET_BARS} bars)");
                    bars.LoadMoreHistory();
                }
                
                var zz = new JFL_ZigZagService(bars);
                
                lock (_syncLock)
                {
                    if (!_bars.ContainsKey(tf)) _bars.Add(tf, bars);
                    if (!_zigzags.ContainsKey(tf)) _zigzags.Add(tf, zz);
                    _cachedActiveTimeFrames = _zigzags.Keys.ToList();
                }
                
                Action<BarsHistoryLoadedEventArgs> handler = (args) => HandleHistoryLoaded(tf, bars, zz, args);
                lock (_syncLock)
                {
                    if (!_historyHandlers.ContainsKey(tf))
                        _historyHandlers.Add(tf, handler);
                }
                bars.HistoryLoaded += handler;
                
                if (bars.Count > 0)
                {
                    zz.Calculate();
                    lock (_syncLock)
                    {
                        _lastBarCount[tf] = bars.Count;
                    }
                    if (_debugMode)
                        _print($"✅ MTF: Initialized {tf} | Bars: {bars.Count} | Pivots: {zz.PivotCount}");

                    // Disparar siempre que haya datos — W/M nunca alcanzarán 2000 barras
                    if (bars.Count >= MTF_TARGET_BARS || bars.Count > 0)
                        OnHistoryLoaded?.Invoke(tf);
                }
                else
                {
                    if (_debugMode) _print($"⚠️ MTF: TimeFrame {tf} has 0 bars!");
                }
            }
            catch (Exception ex)
            {
                if (_debugMode) _print($"❌ MTF Init Error ({tf}): {ex.Message}");
            }
        }

        private void HandleHistoryLoaded(TimeFrame tf, Bars bars, JFL_ZigZagService zz, BarsHistoryLoadedEventArgs args)
        {
             if (_isDisconnected) return;
             try
             {
                 _robot.BeginInvokeOnMainThread(() =>
                 {
                     try
                     {
                         if (_isDisconnected) return;

                         zz.Calculate();
                         
                         if (bars.Count < MTF_TARGET_BARS && args.Count > 0)
                         {
                             if (_debugMode) _print($"📜 MTF ({tf}): Data arrived ({bars.Count}/{MTF_TARGET_BARS}). Requesting more...");
                             bars.LoadMoreHistory();
                         }
                         
                         lock (_syncLock)
                         {
                             _lastBarCount[tf] = bars.Count;
                         }
                         
                         OnHistoryLoaded?.Invoke(tf);
                     }
                     catch (Exception ex)
                     {
                         if (_debugMode) _print($"❌ Async MTF History Delegate Error ({tf}): {ex.Message}");
                     }
                 });
             }
             catch (Exception ex)
             {
                 if (_debugMode) _print($"❌ Async MTF History Error ({tf}): {ex.Message}");
             }
        }
        
        private int _lastUpdateTick = 0;
        
        // Returns a list of TimeFrames where a bar just closed
        public List<TimeFrame> UpdateAll()
        {
            var closedTfs = new List<TimeFrame>();
            
            int currentTick = Environment.TickCount;
            // Handle wrap-around gracefully (TickCount wraps every ~24.9 days)
            if (currentTick - _lastUpdateTick < 100 && currentTick >= _lastUpdateTick) return closedTfs;
            _lastUpdateTick = currentTick;

            lock (_syncLock)
            {
                foreach (var kvp in _zigzags)
                {
                    var tf = kvp.Key;
                    var zz = kvp.Value;

                    if (!_bars.ContainsKey(tf)) continue;
                    var bars = _bars[tf];

                    if (bars == null || zz == null) continue;

                    if (_lastBarCount.TryGetValue(tf, out int lastCount) && bars.Count != lastCount)
                    {
                        _lastBarCount[tf] = bars.Count;
                        closedTfs.Add(tf);
                    }

                    // Always recalculate to keep current-bar extreme current
                    zz.Calculate();
                }
            }

            IsReady = true;
            return closedTfs;
        }

        
        public JFL_ZigZagService GetZigZag(TimeFrame tf)
        {
            lock (_syncLock)
            {
                if (_zigzags.ContainsKey(tf))
                    return _zigzags[tf];
            }
            return null;
        }
        
        public Bars GetBars(TimeFrame tf)
        {
            lock (_syncLock)
            {
                if (_bars.ContainsKey(tf))
                    return _bars[tf];
            }
            return null;
        }
        
        public List<TimeFrame> GetActiveTimeFrames()
        {
            return _cachedActiveTimeFrames;
        }

        public void Disconnect()
        {
            _isDisconnected = true;

            lock (_syncLock)
            {
                if (_bars != null && _historyHandlers != null)
                {
                    foreach (var kvp in _bars)
                    {
                        TimeFrame tf = kvp.Key;
                        Bars bars = kvp.Value;
                        if (_historyHandlers.TryGetValue(tf, out var handler))
                        {
                            bars.HistoryLoaded -= handler;
                        }
                    }
                    _historyHandlers.Clear();
                    _bars.Clear();
                }

                _zigzags?.Clear();
                _lastBarCount?.Clear();
            }
        }
    }
}
