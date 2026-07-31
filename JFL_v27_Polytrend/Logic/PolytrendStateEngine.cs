using System;
using System.Collections.Generic;
using cAlgo.API;
using cAlgo.API.Internals;

namespace cAlgo.Robots.JFL_v27_Polytrend
{
    /// <summary>
    /// Resolves the latest G/L/SG/RL state and valid-side tests for every level.
    /// State is recomputed from closed bars, so historical reloads and live updates
    /// always produce the same result.
    /// </summary>
    public sealed class PolytrendStateEngine
    {
        private readonly Symbol _symbol;
        private readonly bool _useWicksForStateChanges;
        private readonly double _offsetPips;

        public PolytrendStateEngine(Symbol symbol, bool useWicksForStateChanges,
            double offsetPips)
        {
            _symbol = symbol;
            _useWicksForStateChanges = useWicksForStateChanges;
            _offsetPips = offsetPips;
        }

        public void Evaluate(List<PolytrendResult> levels, Bars bars)
        {
            if (levels == null || bars == null || bars.Count < 2)
                return;

            int lastClosedBar = bars.Count - 2;
            foreach (var level in levels)
                EvaluateLevel(level, bars, lastClosedBar);
        }

        private void EvaluateLevel(PolytrendResult level, Bars bars, int lastClosedBar)
        {
            int pivotBar = bars.OpenTimes.GetIndexByTime(level.PivotTime);
            if (pivotBar < 0 || pivotBar > lastClosedBar)
            {
                ApplyState(level, level.IsResistance ? false : true, level.PivotTime);
                return;
            }

            bool isAbove = IsAbove(level, bars, pivotBar, level.IsResistance ? false : true);
            int stateStartBar = pivotBar;
            for (int bar = pivotBar + 1; bar <= lastClosedBar; bar++)
            {
                bool nextSide = IsAbove(level, bars, bar, isAbove);
                if (nextSide == isAbove)
                    continue;

                isAbove = nextSide;
                stateStartBar = bar;
            }

            ApplyState(level, isAbove, bars.OpenTimes[stateStartBar]);
            ApplyTests(level, bars, stateStartBar, lastClosedBar, isAbove);
        }

        private bool IsAbove(PolytrendResult level, Bars bars, int bar, bool previousSide)
        {
            double offset = _offsetPips * _symbol.PipSize;
            if (!_useWicksForStateChanges)
            {
                if (bars.ClosePrices[bar] >= level.LinePrice + offset) return true;
                if (bars.ClosePrices[bar] <= level.LinePrice - offset) return false;
                return previousSide;
            }

            bool wickAbove = bars.HighPrices[bar] >= level.LinePrice + offset;
            bool wickBelow = bars.LowPrices[bar] <= level.LinePrice - offset;
            if (wickAbove && !wickBelow) return true;
            if (wickBelow && !wickAbove) return false;

            // A bar spanning the level has no unambiguous wick-side. Its close
            // gives a deterministic tie-breaker and prevents flickering states.
            if (bars.ClosePrices[bar] >= level.LinePrice + offset) return true;
            if (bars.ClosePrices[bar] <= level.LinePrice - offset) return false;
            return previousSide;
        }

        private void ApplyState(PolytrendResult level, bool isAbove, DateTime changedAt)
        {
            level.State = isAbove
                ? (level.Role == LevelRole.Support
                    ? PolytrendLevelState.SupportGained
                    : PolytrendLevelState.Gained)
                : (level.Role == LevelRole.Support
                    ? PolytrendLevelState.Lost
                    : PolytrendLevelState.ResistanceLost);
            level.StateChangedAt = changedAt;
        }

        private void ApplyTests(PolytrendResult level, Bars bars, int stateStartBar,
            int lastClosedBar, bool isAbove)
        {
            level.IsTested = false;
            level.LastTestTime = null;
            level.TestCount = 0;

            double offset = _offsetPips * _symbol.PipSize;
            for (int bar = stateStartBar + 1; bar <= lastClosedBar; bar++)
            {
                bool tested = isAbove
                    // Support side: dip to the level and close back above it.
                    ? bars.LowPrices[bar] <= level.LinePrice + offset &&
                      bars.ClosePrices[bar] >= level.LinePrice
                    // Resistance side: rally to the level and close back below it.
                    : bars.HighPrices[bar] >= level.LinePrice - offset &&
                      bars.ClosePrices[bar] <= level.LinePrice;

                if (!tested)
                    continue;

                level.IsTested = true;
                level.LastTestTime = bars.OpenTimes[bar];
                level.TestCount++;
            }
        }
    }
}
