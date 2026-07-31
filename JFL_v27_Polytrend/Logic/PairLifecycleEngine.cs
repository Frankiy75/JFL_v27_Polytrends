using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Internals;

namespace cAlgo.Robots.JFL_v27_Polytrend
{
    /// <summary>
    /// Captures the behaviour described by Polytrend: after a support is lost,
    /// its paired resistance is the level to watch on the first rally; after a
    /// resistance is gained, its paired support is the level to watch on the
    /// first pullback.  A later revisit remains valid, but is no longer fresh.
    /// </summary>
    public sealed class PairLifecycleEngine
    {
        private readonly Symbol _symbol;
        private readonly double _offsetPips;

        public PairLifecycleEngine(Symbol symbol, double offsetPips)
        {
            _symbol = symbol;
            _offsetPips = offsetPips;
        }

        public void Evaluate(List<PolytrendPair> pairs, Bars bars)
        {
            if (pairs == null || pairs.Count == 0)
                return;

            var ordered = pairs.OrderBy(p => p.EndTime).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                var pair = ordered[i];
                pair.PreviousPairId = i > 0 ? ordered[i - 1].PairId : null;
                pair.NextPairId = i < ordered.Count - 1 ? ordered[i + 1].PairId : null;
                EvaluatePair(pair, bars);
            }
        }

        private void EvaluatePair(PolytrendPair pair, Bars bars)
        {
            pair.LifecycleState = PairLifecycleState.Neutral;
            pair.TriggerLevelId = null;
            pair.ExpectedRetestLevelId = null;
            pair.LifecycleStartedAt = DateTime.MinValue;
            pair.ExpectedRetestCount = 0;
            pair.LastExpectedRetestTime = null;

            if (pair.Support == null || pair.Resistance == null || bars == null || bars.Count < 2)
                return;

            bool supportLost = pair.Support.State == PolytrendLevelState.Lost;
            bool resistanceGained = pair.Resistance.State == PolytrendLevelState.Gained;
            if (!supportLost && !resistanceGained)
                return;

            // When both members changed role, the latest change is the active
            // event. It is the only event that can still have a fresh return.
            bool useLostSupport = supportLost &&
                (!resistanceGained || pair.Support.StateChangedAt >= pair.Resistance.StateChangedAt);
            var trigger = useLostSupport ? pair.Support : pair.Resistance;
            var expected = useLostSupport ? pair.Resistance : pair.Support;

            pair.TriggerLevelId = trigger.LevelId;
            pair.ExpectedRetestLevelId = expected.LevelId;
            // A pair is actionable only once both pivots that define it have
            // been confirmed. This prevents a historical bar from being
            // counted as a retest of a partner that did not yet exist.
            pair.LifecycleStartedAt = trigger.StateChangedAt > pair.EndTime
                ? trigger.StateChangedAt
                : pair.EndTime;

            int lastClosed = bars.Count - 2;
            for (int bar = 0; bar <= lastClosed; bar++)
            {
                if (bars.OpenTimes[bar] <= pair.LifecycleStartedAt)
                    continue;
                if (!IsExpectedRetest(bars, bar, expected, useLostSupport))
                    continue;

                pair.ExpectedRetestCount++;
                pair.LastExpectedRetestTime = bars.OpenTimes[bar];
            }

            if (useLostSupport)
                pair.LifecycleState = pair.ExpectedRetestCount == 0
                    ? PairLifecycleState.SupportLostAwaitingRetest
                    : PairLifecycleState.SupportLostRetested;
            else
                pair.LifecycleState = pair.ExpectedRetestCount == 0
                    ? PairLifecycleState.ResistanceGainedAwaitingRetest
                    : PairLifecycleState.ResistanceGainedRetested;
        }

        private bool IsExpectedRetest(Bars bars, int bar, PolytrendResult expected,
            bool supportWasLost)
        {
            double offset = _offsetPips * _symbol.PipSize;
            return supportWasLost
                // The former pair is now resistance: rally into it and reject.
                ? bars.HighPrices[bar] >= expected.LinePrice - offset &&
                  bars.ClosePrices[bar] <= expected.LinePrice
                // The former pair is now support: pull back into it and hold.
                : bars.LowPrices[bar] <= expected.LinePrice + offset &&
                  bars.ClosePrices[bar] >= expected.LinePrice;
        }
    }
}
