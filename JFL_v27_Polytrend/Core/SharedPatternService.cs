using System;
using System.Collections.Generic;
using cAlgo.API;

namespace cAlgo.Robots.JFL_v27_Polytrend
{
    /// <summary>
    /// Static service to share scan results between bot instances in the same process.
    /// Allows MTF systems to use "original" results from active charts.
    /// </summary>
    public static class SharedPatternService
    {
        // Stores patterns per Symbol and TimeFrame name (Primitive string to avoid AppDomain crashes)
        private static readonly Dictionary<string, Dictionary<string, List<PolytrendResult>>> _store = 
            new Dictionary<string, Dictionary<string, List<PolytrendResult>>>();
        private static readonly Dictionary<string, Dictionary<string, List<PolytrendPair>>> _pairStore =
            new Dictionary<string, Dictionary<string, List<PolytrendPair>>>();

        // Event to notify when patterns are updated for a symbol/timeframe
        // Signature uses string instead of TimeFrame object to survive recompilation safely
        public static event Action<string, string> OnPatternUpdated;

        public static void RegisterResults(string symbol, string tfName, List<PolytrendResult> results)
        {
            RegisterResults(symbol, tfName, results, null);
        }

        public static void RegisterResults(string symbol, string tfName, List<PolytrendResult> results,
            List<PolytrendPair> pairs)
        {
            lock (_store)
            {
                if (!_store.ContainsKey(symbol))
                    _store[symbol] = new Dictionary<string, List<PolytrendResult>>();
                
                // Store a copy to prevent accidental modifications from other threads
                _store[symbol][tfName] = new List<PolytrendResult>(results);

                if (!_pairStore.ContainsKey(symbol))
                    _pairStore[symbol] = new Dictionary<string, List<PolytrendPair>>();
                _pairStore[symbol][tfName] = pairs == null
                    ? new List<PolytrendPair>()
                    : new List<PolytrendPair>(pairs);
            }
            
            // Notify subscribers one by one with individual error guards.
            // If a single subscriber throws (e.g. disposed bot), others still receive the update.
            var evt = OnPatternUpdated;
            if (evt != null)
            {
                foreach (Action<string, string> handler in evt.GetInvocationList())
                {
                    try { handler(symbol, tfName); }
                    catch { /* Subscriber error isolated - do not crash caller */ }
                }
            }
        }

        public static List<PolytrendResult> GetResults(string symbol, string tfName)
        {
            lock (_store)
            {
                if (_store.ContainsKey(symbol) && _store[symbol].ContainsKey(tfName))
                {
                    return new List<PolytrendResult>(_store[symbol][tfName]);
                }
            }
            return null;
        }

        public static List<PolytrendPair> GetPairs(string symbol, string tfName)
        {
            lock (_store)
            {
                if (_pairStore.ContainsKey(symbol) && _pairStore[symbol].ContainsKey(tfName))
                    return new List<PolytrendPair>(_pairStore[symbol][tfName]);
            }
            return null;
        }

        // Global blacklist to sync invalidations across different bot instances
        private static readonly HashSet<string> _blacklist = new HashSet<string>();
        // ✅ FREEZE FIX: Queue for O(1) FIFO eviction — Enumerable.First() on a 1000-item HashSet
        // inside a static lock was O(n) and contended by all open chart instances simultaneously.
        private static readonly Queue<string> _blacklistOrder = new Queue<string>();
        private const int _blacklistMaxSize = 1000;

        public static void RegisterBlacklist(string symbol, string tf, long pivotTicks, bool isResistance)
        {
            string type = isResistance ? "R" : "S";
            string key = $"{symbol}:{tf}:{pivotTicks}:{type}";
            lock (_blacklist)
            {
                if (!_blacklist.Contains(key))
                {
                    if (_blacklist.Count >= _blacklistMaxSize)
                    {
                        string oldest = _blacklistOrder.Dequeue();
                        _blacklist.Remove(oldest);
                    }
                    _blacklist.Add(key);
                    _blacklistOrder.Enqueue(key);
                }
            }
        }

        public static bool IsBlacklisted(string symbol, string tf, long pivotTicks, bool isResistance)
        {
            string type = isResistance ? "R" : "S";
            string key = $"{symbol}:{tf}:{pivotTicks}:{type}";
            lock (_blacklist)
            {
                return _blacklist.Contains(key);
            }
        }

        // --- MASTER CHART REGISTRY ---
        // Tracks which symbols and timeframes have an active chart instance running the bot.
        // Used by other instances to skip redundant (and potentially inconsistent) headless scans.
        private static readonly Dictionary<string, HashSet<string>> _masterRegistry = 
            new Dictionary<string, HashSet<string>>();

        public static void RegisterMaster(string symbol, string tfName)
        {
            lock (_masterRegistry)
            {
                if (!_masterRegistry.ContainsKey(symbol))
                    _masterRegistry[symbol] = new HashSet<string>();
                _masterRegistry[symbol].Add(tfName);
            }
        }

        public static void UnregisterMaster(string symbol, string tfName)
        {
            lock (_masterRegistry)
            {
                if (_masterRegistry.ContainsKey(symbol))
                    _masterRegistry[symbol].Remove(tfName);
            }
        }

        public static bool IsMasterActive(string symbol, string tfName)
        {
            lock (_masterRegistry)
            {
                return _masterRegistry.ContainsKey(symbol) && _masterRegistry[symbol].Contains(tfName);
            }
        }

        public static void Clear()
        {
            lock (_store) { _store.Clear(); _pairStore.Clear(); }
            lock (_blacklist) { _blacklist.Clear(); _blacklistOrder.Clear(); }
            lock (_masterRegistry) _masterRegistry.Clear();
        }
    }
}
