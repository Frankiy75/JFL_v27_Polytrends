using System.Collections.Generic;
using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Internals;

namespace cAlgo.Robots.JFL_v27_Polytrend
{
    public class PolytrendScanner
    {
        private readonly double _minimumLegAtr;
        private readonly double _minimumRetracementRatio;

        public PolytrendScanner(double minimumLegAtr = 1.25,
            double minimumRetracementPercent = 61.8)
        {
            _minimumLegAtr = Math.Max(0.10, minimumLegAtr);
            _minimumRetracementRatio = Math.Max(0.10, minimumRetracementPercent / 100.0);
        }

        public List<PolytrendResult> Scan(JFL_ZigZagService zigzag)
        {
            return ScanWithPairs(zigzag, null, null, null).Levels;
        }

        public PolytrendScanResult ScanWithPairs(JFL_ZigZagService zigzag, Bars bars,
            PolytrendStateEngine stateEngine, PairLifecycleEngine lifecycleEngine)
        {
            var scan = new PolytrendScanResult();
            if (zigzag == null || zigzag.PivotCount == 0) return scan;

            foreach (var pivot in zigzag.Pivots)
                scan.Levels.Add(CreateResult(pivot));

            var selectedLegs = SelectPolytrendLegs(zigzag.Pivots, bars);
            foreach (var selection in selectedLegs)
            {
                int i = selection.StartIndex;
                var firstPivot = zigzag.Pivots[i];
                var secondPivot = zigzag.Pivots[i + 1];
                var firstLevel = scan.Levels[i];
                var secondLevel = scan.Levels[i + 1];

                var support = firstLevel.IsResistance ? secondLevel : firstLevel;
                var resistance = firstLevel.IsResistance ? firstLevel : secondLevel;
                string pairId = $"{firstLevel.LevelId}:{secondLevel.LevelId}";

                firstLevel.PairIds.Add(pairId);
                secondLevel.PairIds.Add(pairId);
                scan.Pairs.Add(new PolytrendPair
                {
                    PairId = pairId,
                    Support = support,
                    Resistance = resistance,
                    StartTime = firstPivot.Time,
                    EndTime = secondPivot.Time,
                    IsBullishMove = !firstPivot.IsHigh && secondPivot.IsHigh,
                    LegStrengthAtr = selection.StrengthAtr,
                    SourceAtr = selection.SourceAtr
                });
            }

            stateEngine?.Evaluate(scan.Levels, bars);
            lifecycleEngine?.Evaluate(scan.Pairs, bars);
            ClassifyTrendActivity(scan.Pairs);
            MarkExternalTrendOfRelevance(zigzag.Pivots, bars, scan.Pairs);
            return scan;
        }

        private static void ClassifyTrendActivity(List<PolytrendPair> pairs)
        {
            if (pairs == null || pairs.Count == 0)
                return;

            foreach (var pair in pairs)
            {
                pair.IsTrendOfRelevance = false;
                pair.IsMainTrendSegment = false;
                bool invalidated = pair.IsBullishMove
                    // A bullish leg loses meaning after losing the support from
                    // which it started.
                    ? pair.Support?.State == PolytrendLevelState.Lost
                    // A bearish leg loses meaning after regaining the
                    // resistance from which it started.
                    : pair.Resistance?.State == PolytrendLevelState.Gained;

                pair.IsTrendInvalidated = invalidated;
                pair.IsTrendActive = !invalidated;
                pair.TrendInvalidatedAt = invalidated
                    ? (pair.IsBullishMove ? pair.Support?.StateChangedAt : pair.Resistance?.StateChangedAt)
                    : null;
            }

            // The dotted leg is the latest selected trend still structurally
            // active, not merely the latest internal ZigZag swing.
            var currentTrend = pairs.Where(p => p.IsTrendActive)
                .OrderByDescending(p => p.EndTime)
                .FirstOrDefault()
                ?? pairs.OrderByDescending(p => p.EndTime).First();
            currentTrend.IsTrendOfRelevance = true;
        }

        private void MarkExternalTrendOfRelevance(List<JFL_Pivot> pivots, Bars bars,
            List<PolytrendPair> pairs)
        {
            if (pivots == null || pivots.Count < 2 || pairs == null || pairs.Count == 0)
                return;

            var externalLegs = BuildExternalLegs(pivots, bars);
            if (externalLegs.Count == 0)
            {
                // Quiet/short histories may not yet have a confirmed external
                // reversal. Keep a visible dotted reference instead of
                // disappearing until the next large swing forms.
                var fallback = pairs.Where(pair => pair.IsTrendActive)
                    .OrderByDescending(pair => pair.LegStrengthAtr)
                    .ThenByDescending(pair => pair.EndTime)
                    .FirstOrDefault()
                    ?? pairs.OrderByDescending(pair => pair.EndTime).First();
                foreach (var pair in pairs)
                {
                    pair.IsTrendOfRelevance = false;
                    pair.IsMainTrendSegment = false;
                }
                fallback.IsTrendOfRelevance = true;
                fallback.IsMainTrendSegment = true;
                return;
            }

            // The parent leg is the impulse whose origin is still defending
            // current price. That is the leg carrying the market, rather than
            // simply the most recent completed counter-move.
            double currentPrice = bars != null && bars.Count >= 2
                ? bars.ClosePrices[bars.Count - 2]
                : pivots[pivots.Count - 1].ClosePrice;
            var activeLegs = externalLegs.Where(leg =>
            {
                var start = pivots[leg.StartIndex];
                bool bullish = pivots[leg.EndIndex].Price >= start.Price;
                double basePrice = bullish ? start.BodyBottom : start.BodyTop;
                return bullish ? currentPrice >= basePrice : currentPrice <= basePrice;
            }).ToList();
            var mainLeg = (activeLegs.Count > 0 ? activeLegs : externalLegs)
                .OrderBy(leg =>
                {
                    var start = pivots[leg.StartIndex];
                    bool bullish = pivots[leg.EndIndex].Price >= start.Price;
                    double basePrice = bullish ? start.BodyBottom : start.BodyTop;
                    return Math.Abs(currentPrice - basePrice);
                })
                .ThenByDescending(leg => leg.StrengthAtr)
                .ThenByDescending(leg => leg.EndIndex)
                .First();
            foreach (var pair in pairs)
            {
                pair.IsTrendOfRelevance = false;
                pair.IsMainTrendSegment = false;
            }

            DateTime pathStart = pivots[mainLeg.StartIndex].Time;
            DateTime pathEnd = pivots[mainLeg.EndIndex].Time;
            var pathPairs = pairs.Where(pair => pair.StartTime >= pathStart && pair.EndTime <= pathEnd)
                .OrderBy(pair => pair.EndTime)
                .ToList();
            foreach (var pair in pathPairs)
                pair.IsMainTrendSegment = true;

            var carrier = pathPairs.LastOrDefault()
                ?? pairs.OrderBy(pair => Math.Abs((pair.EndTime - pathEnd).TotalSeconds)).First();
            carrier.IsMainTrendSegment = true;
            carrier.IsTrendOfRelevance = true;
        }

        private List<ExternalLeg> BuildExternalLegs(List<JFL_Pivot> pivots, Bars bars)
        {
            var external = new List<ExternalLeg>();
            int anchorIndex = 0;
            int candidateIndex = 1;

            for (int nextIndex = 2; nextIndex < pivots.Count; nextIndex++)
            {
                var anchor = pivots[anchorIndex];
                var candidate = pivots[candidateIndex];
                var next = pivots[nextIndex];
                bool bullish = candidate.Price >= anchor.Price;
                bool extendsLeg = bullish ? next.Price > candidate.Price : next.Price < candidate.Price;
                if (extendsLeg)
                {
                    candidateIndex = nextIndex;
                    continue;
                }

                double legRange = Math.Abs(candidate.Price - anchor.Price);
                double reversalRange = Math.Abs(next.Price - candidate.Price);
                double legAtr = GetAtrAt(bars, candidate.Time, legRange);
                double reversalAtr = GetAtrAt(bars, next.Time, reversalRange);
                double legStrength = legAtr > 0 ? legRange / legAtr : 0;
                double reversalStrength = reversalAtr > 0 ? reversalRange / reversalAtr : 0;
                bool reversalConfirmed = legRange > 0 &&
                    legStrength >= _minimumLegAtr &&
                    reversalStrength >= _minimumLegAtr &&
                    reversalRange / legRange >= _minimumRetracementRatio;
                if (!reversalConfirmed)
                    continue;

                external.Add(new ExternalLeg
                {
                    StartIndex = anchorIndex,
                    EndIndex = candidateIndex,
                    StrengthAtr = legStrength
                });
                anchorIndex = candidateIndex;
                candidateIndex = nextIndex;
            }

            var finalRange = Math.Abs(pivots[candidateIndex].Price - pivots[anchorIndex].Price);
            var finalAtr = GetAtrAt(bars, pivots[candidateIndex].Time, finalRange);
            var finalStrength = finalAtr > 0 ? finalRange / finalAtr : 0;
            if (finalStrength >= _minimumLegAtr)
            {
                external.Add(new ExternalLeg
                {
                    StartIndex = anchorIndex,
                    EndIndex = candidateIndex,
                    StrengthAtr = finalStrength
                });
            }
            return external;
        }

        private List<LegSelection> SelectPolytrendLegs(List<JFL_Pivot> pivots, Bars bars)
        {
            var selected = new List<LegSelection>();
            if (pivots == null || pivots.Count < 2)
                return selected;

            for (int i = 0; i < pivots.Count - 1; i++)
            {
                var start = pivots[i];
                var end = pivots[i + 1];
                bool bullish = !start.IsHigh && end.IsHigh;
                double range = Math.Abs(end.Price - start.Price);
                double atr = GetAtrAt(bars, end.Time, range);
                double strength = atr > 0 ? range / atr : 0;

                // A trend either extends the prior same-side structural extreme
                // or retraces a meaningful share of the preceding trend. This
                // intentionally discards small internal ZigZag noise.
                bool breaksStructure = i == 0 || (bullish
                    ? end.Price > pivots[i - 1].Price
                    : end.Price < pivots[i - 1].Price);
                double previousRange = i > 0
                    ? Math.Abs(pivots[i].Price - pivots[i - 1].Price)
                    : 0;
                bool meaningfulRetracement = previousRange > 0 &&
                    range / previousRange >= _minimumRetracementRatio;

                // A break of the prior same-side extreme is structural in any
                // timeframe. Do not discard it just because a single global
                // ATR threshold is too demanding on Daily/Weekly data. The
                // threshold still filters internal retracements that did not
                // make a structural break.
                bool isStructuralLeg = breaksStructure ||
                    (meaningfulRetracement && strength >= _minimumLegAtr);
                if (isStructuralLeg)
                    selected.Add(new LegSelection
                    {
                        StartIndex = i,
                        StrengthAtr = strength,
                        SourceAtr = atr
                    });
            }

            // Never leave the chart without a current trend merely because a
            // quiet market has not yet met the configured displacement.
            if (selected.Count == 0)
            {
                int latest = pivots.Count - 2;
                selected.Add(new LegSelection
                {
                    StartIndex = latest,
                    StrengthAtr = 1.0,
                    SourceAtr = Math.Abs(pivots[latest + 1].Price - pivots[latest].Price)
                });
            }
            return selected;
        }

        private static double GetAtrAt(Bars bars, DateTime time, double fallbackRange)
        {
            if (bars == null || bars.Count < 2)
                return fallbackRange;

            int end = bars.OpenTimes.GetIndexByTime(time);
            if (end < 1) end = Math.Min(bars.Count - 2, Math.Max(1, bars.Count - 1));
            end = Math.Min(end, bars.Count - 2);
            int start = Math.Max(1, end - 13);
            double sum = 0;
            int count = 0;
            for (int bar = start; bar <= end; bar++)
            {
                double previousClose = bars.ClosePrices[bar - 1];
                double tr = Math.Max(bars.HighPrices[bar] - bars.LowPrices[bar],
                    Math.Max(Math.Abs(bars.HighPrices[bar] - previousClose),
                        Math.Abs(bars.LowPrices[bar] - previousClose)));
                sum += tr;
                count++;
            }
            return count > 0 ? sum / count : fallbackRange;
        }

        private sealed class LegSelection
        {
            public int StartIndex { get; set; }
            public double StrengthAtr { get; set; }
            public double SourceAtr { get; set; }
        }

        private sealed class ExternalLeg
        {
            public int StartIndex { get; set; }
            public int EndIndex { get; set; }
            public double StrengthAtr { get; set; }
        }

        private PolytrendResult CreateResult(JFL_Pivot pivot)
        {
            double linePrice = pivot.IsHigh ? pivot.BodyTop : pivot.BodyBottom;
            bool isResistance = pivot.IsHigh;

            return new PolytrendResult
            {
                LevelId      = $"{pivot.Time.Ticks}_{(isResistance ? "R" : "S")}",
                IsValid      = true,
                IsResistance = isResistance,
                Type         = isResistance ? "RESISTANCE" : "SUPPORT",
                PivotTime    = pivot.Time,
                LinePrice    = linePrice,
                PivotPrice   = pivot.Price,
                Role         = isResistance ? LevelRole.Resistance : LevelRole.Support
            };
        }
    }
}
