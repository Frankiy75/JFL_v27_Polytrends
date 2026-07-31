using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;

namespace cAlgo.Robots.JFL_v27_Polytrend
{
    public class PolytrendLineManager
    {
        private readonly Robot _robot;
        private readonly string _prefix;
        private LineTouchDetector _touchDetector;
        private MTFZigZagService _mtfZigZag;

        private readonly int _untestedLineWidth;
        private readonly int _testedLineWidth;
        private bool _classicMode;
        private int _oldPairStubBars;
        private int _maxZigZagConnectors = 3;
        private static readonly Color[] PairPalette =
        {
            Color.FromArgb(255,  70, 170, 255),
            Color.FromArgb(255, 255,  90, 180),
            Color.FromArgb(255, 255, 200,  60),
            Color.FromArgb(255,  85, 220, 150),
            Color.FromArgb(255, 190, 120, 255)
        };

        public PolytrendLineManager(Robot robot, int untestedLineWidth = 1, int testedLineWidth = 1, string prefix = "PT_")
        {
            _robot = robot;
            _untestedLineWidth = untestedLineWidth;
            _testedLineWidth = testedLineWidth;
            _prefix = prefix;
        }

        public void SetTouchDetector(LineTouchDetector detector, MTFZigZagService mtfZigZag)
        {
            _touchDetector = detector;
            _mtfZigZag = mtfZigZag;
        }

        public void SetVisualOptions(bool classicMode, int oldPairStubBars, int maxZigZagConnectors)
        {
            _classicMode = classicMode;
            _oldPairStubBars = oldPairStubBars;
            _maxZigZagConnectors = Math.Max(0, maxZigZagConnectors);
        }

        public void ClearTimeFrame(string timeframe)
        {
            var toRemove = _robot.Chart.Objects.Where(o => o.Name.StartsWith($"{_prefix}{timeframe}_")).ToList();
            foreach (var obj in toRemove) _robot.Chart.RemoveObject(obj.Name);
        }

        public void DrawLevels(List<PolytrendResult> levels, string timeframe,
            Color supportColor, Color resistanceColor, DateTime vertLineDate)
        {
            DrawLevels(levels, null, timeframe, supportColor, resistanceColor, vertLineDate);
        }

        public void DrawLevels(List<PolytrendResult> levels, List<PolytrendPair> pairs,
            string timeframe, Color supportColor, Color resistanceColor, DateTime vertLineDate)
        {
            var objectsToRemove = _robot.Chart.Objects.Where(o => o.Name.StartsWith($"{_prefix}{timeframe}_")).ToList();
            foreach (var obj in objectsToRemove) _robot.Chart.RemoveObject(obj.Name);

            if (levels == null) return;

            var visiblePairs = GetVisiblePairs(levels, pairs);
            var pairColors = AssignPairColors(visiblePairs);
            var levelColors = GetLevelColors(visiblePairs, pairColors);
            var connectorPairIds = new HashSet<string>(visiblePairs
                .Take(_maxZigZagConnectors)
                .Select(p => p.PairId));

            int count = 0;
            foreach (var level in levels.OrderByDescending(l => l.PivotTime))
                DrawLevel(level, timeframe, $"{_prefix}{timeframe}_{count++}",
                    supportColor, resistanceColor, vertLineDate,
                    levelColors.TryGetValue(level.LevelId, out var pairColor) ? pairColor : null);

            foreach (var pair in visiblePairs)
                DrawPairConnector(pair, timeframe, $"{_prefix}{timeframe}_pair_{pair.PairId}",
                    pairColors[pair.PairId], vertLineDate, connectorPairIds.Contains(pair.PairId));
        }

        public void DrawMtfPool(List<PolytrendResult> levels, IEnumerable<string> mtfNames,
            Color supportColor, Color resistanceColor,
            string chartTfName, DateTime vertLineDate)
        {
            DrawMtfPool(levels, null, mtfNames, supportColor, resistanceColor, chartTfName, vertLineDate);
        }

        public void DrawMtfPool(List<PolytrendResult> levels, List<PolytrendPair> pairs,
            IEnumerable<string> mtfNames, Color supportColor, Color resistanceColor,
            string chartTfName, DateTime vertLineDate)
        {
            foreach (var tf in mtfNames)
                ClearTimeFrame(tf);

            if (levels == null) return;

            double chartTfMins = GetTfMinutes(chartTfName);

            if (pairs != null && pairs.Count > 0)
            {
                var pairColors = AssignPairColors(pairs.OrderBy(p => DistanceToPair(p, _robot.Symbol.Bid)).ToList());
                var connectorPairIds = new HashSet<string>(pairs
                    .OrderBy(p => DistanceToPair(p, _robot.Symbol.Bid))
                    .Take(_maxZigZagConnectors)
                    .Select(p => p.PairId));
                var pairCounters = new Dictionary<string, int>();
                foreach (var pair in pairs.OrderBy(p => DistanceToPair(p, _robot.Symbol.Bid)))
                {
                    string tf = pair.TimeframeName;
                    if (!pairCounters.ContainsKey(tf)) pairCounters[tf] = 0;
                    string baseName = $"{_prefix}{tf}_pair_{pairCounters[tf]++}";
                    DateTime labelTime = _robot.Bars.OpenTimes.LastValue
                        .AddMinutes(chartTfMins * (2 + GetTfRank(tf) * 2));
                    Color pairColor = pairColors[pair.PairId];

                    DrawMtfLevel(pair.Support, tf, $"{baseName}_support", pairColor, labelTime, vertLineDate);
                    DrawMtfLevel(pair.Resistance, tf, $"{baseName}_resistance", pairColor, labelTime, vertLineDate);
                    DrawPairConnector(pair, tf, baseName, pairColor, vertLineDate,
                        connectorPairIds.Contains(pair.PairId));
                }
                return;
            }

            var counters = new Dictionary<string, int>();
            foreach (var level in levels.OrderByDescending(l => l.PivotTime))
            {
                string tf = level.TimeframeName;
                if (!counters.ContainsKey(tf)) counters[tf] = 0;
                string baseName = $"{_prefix}{tf}_{counters[tf]++}";
                Color tfColor = GetTfColor(tf);
                DateTime labelTime = _robot.Bars.OpenTimes.LastValue
                    .AddMinutes(chartTfMins * (2 + GetTfRank(tf) * 2));
                DrawMtfLevel(level, tf, baseName, tfColor, labelTime, vertLineDate);
            }
        }

        private void DrawMtfLevel(PolytrendResult level, string timeframe, string baseName,
            Color lineColor, DateTime labelTime, DateTime vertLineDate)
        {
            DateTime farRight = GetLineEndTime(level.PivotTime, timeframe);
            DateTime startTime = (vertLineDate != DateTime.MinValue && level.PivotTime < vertLineDate)
                ? vertLineDate : level.PivotTime;

            bool tested = level.IsTested;
            LineStyle style    = tested ? LineStyle.Lines : LineStyle.Dots;
            int lineWidth      = tested ? _testedLineWidth : _untestedLineWidth;

            Color rayColor = DimRayColor(lineColor, tested ? 145 : 105);
            _robot.Chart.DrawTrendLine(baseName,
                startTime, level.LinePrice,
                farRight,  level.LinePrice,
                rayColor, lineWidth, style);

            _robot.Chart.DrawText($"{baseName}_lbl", GetTimeframeStateText(level, timeframe),
                labelTime, level.LinePrice, lineColor).FontSize = 9;
        }

        private void DrawLevel(PolytrendResult level, string timeframe, string baseName,
            Color supportColor, Color resistanceColor, DateTime vertLineDate, Color pairColor = null)
        {
            Color lineColor = pairColor ?? (level.IsResistance
                ? Color.FromArgb(255, resistanceColor.R, resistanceColor.G, resistanceColor.B)
                : Color.FromArgb(255, supportColor.R, supportColor.G, supportColor.B));

            double tfMins    = GetTfMinutes(timeframe);
            double chartMins = GetTfMinutes(_robot.TimeFrame.ToString());
            DateTime labelTime = _robot.Bars.OpenTimes.LastValue
                .AddMinutes(chartMins * (2 + GetTfRank(timeframe) * 2));

            DateTime farRight = GetLineEndTime(level.PivotTime, timeframe);
            DateTime lineStart = (vertLineDate != DateTime.MinValue && level.PivotTime < vertLineDate)
                ? vertLineDate : level.PivotTime;

            bool tested = level.IsTested;
            LineStyle style = tested ? LineStyle.Lines : LineStyle.Dots;
            int lineWidth   = tested ? _testedLineWidth : _untestedLineWidth;

            Color rayColor = DimRayColor(lineColor, tested ? 145 : 105);
            _robot.Chart.DrawTrendLine(baseName,
                lineStart, level.LinePrice,
                farRight,  level.LinePrice,
                rayColor, lineWidth, style);

            // The origin mark is hidden before the visual cut-off, but the
            // right-side label remains so a clipped pair is still identifiable.
            if (vertLineDate == DateTime.MinValue || level.PivotTime >= vertLineDate)
            {
                DateTime tickStart = level.PivotTime;
                DateTime tickEnd   = tickStart.AddMinutes(tfMins);
                _robot.Chart.DrawTrendLine($"{baseName}_tick",
                    tickStart, level.LinePrice,
                    tickEnd,   level.LinePrice,
                    lineColor, 2, LineStyle.Solid);
            }

            _robot.Chart.DrawText($"{baseName}_lbl", GetTimeframeStateText(level, timeframe),
                labelTime, level.LinePrice, lineColor).FontSize = 10;
        }

        private void DrawPairConnector(PolytrendPair pair, string timeframe, string baseName, Color color,
            DateTime vertLineDate, bool drawZigZagLeg)
        {
            if (pair?.Support == null || pair.Resistance == null)
                return;

            double tfMins = GetTfMinutes(timeframe);
            DateTime supportCapEnd = pair.Support.PivotTime.AddMinutes(tfMins * 1.50);
            DateTime resistanceCapEnd = pair.Resistance.PivotTime.AddMinutes(tfMins * 1.50);
            Color connectorColor = Color.FromArgb(pair.IsPrimary ? 235 : (pair.IsBright ? 215 : 165),
                color.R, color.G, color.B);

            DateTime legStartTime = pair.Support.PivotTime;
            double legStartPrice = pair.Support.PivotPrice;
            DateTime legEndTime = pair.Resistance.PivotTime;
            double legEndPrice = pair.Resistance.PivotPrice;
            if (vertLineDate != DateTime.MinValue && legEndTime <= vertLineDate &&
                !pair.IsMainTrendSegment)
                return;
            if (!pair.IsMainTrendSegment)
                ClipLegAtCutoff(vertLineDate, ref legStartTime, ref legStartPrice, legEndTime, legEndPrice);

            // The coloured leg follows the real ZigZag pivots, clipped at the
            // user-selected vertical boundary; the short marks remain body S/R.
            if (drawZigZagLeg)
                _robot.Chart.DrawTrendLine($"{baseName}_leg",
                    legStartTime, legStartPrice, legEndTime, legEndPrice,
                    connectorColor, GetPairThickness(pair), pair.IsMainTrendSegment ? LineStyle.Dots : LineStyle.Solid);

            // Longer horizontal marks keep each structural level readable at the
            // candle that generated it.
            if (vertLineDate == DateTime.MinValue || pair.Support.PivotTime >= vertLineDate)
                _robot.Chart.DrawTrendLine($"{baseName}_support_anchor",
                    pair.Support.PivotTime, pair.Support.LinePrice,
                    supportCapEnd, pair.Support.LinePrice,
                    connectorColor, GetPairThickness(pair), LineStyle.Solid);
            if (vertLineDate == DateTime.MinValue || pair.Resistance.PivotTime >= vertLineDate)
                _robot.Chart.DrawTrendLine($"{baseName}_resistance_anchor",
                    pair.Resistance.PivotTime, pair.Resistance.LinePrice,
                    resistanceCapEnd, pair.Resistance.LinePrice,
                    connectorColor, GetPairThickness(pair), LineStyle.Solid);

        }

        private static void ClipLegAtCutoff(DateTime cutoff, ref DateTime startTime, ref double startPrice,
            DateTime endTime, double endPrice)
        {
            if (cutoff == DateTime.MinValue || cutoff <= startTime || cutoff >= endTime)
                return;

            double totalSeconds = (endTime - startTime).TotalSeconds;
            if (totalSeconds <= 0)
                return;
            double fraction = (cutoff - startTime).TotalSeconds / totalSeconds;
            startPrice += (endPrice - startPrice) * fraction;
            startTime = cutoff;
        }

        private static int GetPairThickness(PolytrendPair pair)
        {
            if (pair.IsMainTrendSegment) return 4;
            if (pair.IsPrimary) return 4;
            if (pair.IsBright) return 3;
            return 2;
        }

        private static Color DimRayColor(Color color, int alpha)
        {
            return Color.FromArgb(alpha, color.R, color.G, color.B);
        }

        private static string GetTimeframeStateText(PolytrendResult level, string timeframe)
        {
            return TfUtils.GetLabel(timeframe) + " " + GetStateLabel(level.State);
        }

        private List<PolytrendPair> GetVisiblePairs(List<PolytrendResult> levels,
            List<PolytrendPair> pairs)
        {
            if (pairs == null || pairs.Count == 0)
                return new List<PolytrendPair>();

            return pairs
                .Where(p => p.Support != null && p.Resistance != null)
                .OrderBy(p => DistanceToPair(p, _robot.Symbol.Bid))
                .ToList();
        }

        private static Dictionary<string, Color> AssignPairColors(List<PolytrendPair> pairs)
        {
            var colors = new Dictionary<string, Color>();
            for (int i = 0; i < pairs.Count; i++)
            {
                Color baseColor = PairPalette[i % PairPalette.Length];
                colors[pairs[i].PairId] = Color.FromArgb(pairs[i].VisualOpacity,
                    baseColor.R, baseColor.G, baseColor.B);
            }
            return colors;
        }

        private static Dictionary<string, Color> GetLevelColors(List<PolytrendPair> pairs,
            Dictionary<string, Color> pairColors)
        {
            var colors = new Dictionary<string, Color>();
            foreach (var pair in pairs)
            {
                // Pairs are ordered by relevance; the first one owns a shared pivot's colour.
                if (!colors.ContainsKey(pair.Support.LevelId))
                    colors[pair.Support.LevelId] = pairColors[pair.PairId];
                if (!colors.ContainsKey(pair.Resistance.LevelId))
                    colors[pair.Resistance.LevelId] = pairColors[pair.PairId];
            }
            return colors;
        }

        private static double DistanceToPair(PolytrendPair pair, double price)
        {
            if (price < pair.Support.LinePrice) return pair.Support.LinePrice - price;
            if (price > pair.Resistance.LinePrice) return price - pair.Resistance.LinePrice;
            return 0;
        }

        private static string GetStateLabel(PolytrendLevelState state)
        {
            switch (state)
            {
                case PolytrendLevelState.Gained: return "G";
                case PolytrendLevelState.Lost: return "L";
                case PolytrendLevelState.SupportGained: return "SG";
                case PolytrendLevelState.ResistanceLost: return "RL";
                default: return string.Empty;
            }
        }

        private static TimeFrame ParseTimeFrame(string tfName)
        {
            switch (tfName)
            {
                case "Minute":   return TimeFrame.Minute;
                case "Minute3":  return TimeFrame.Minute3;
                case "Minute5":  return TimeFrame.Minute5;
                case "Minute15": return TimeFrame.Minute15;
                case "Hour":     return TimeFrame.Hour;
                case "Hour4":    return TimeFrame.Hour4;
                case "Hour12":   return TimeFrame.Hour12;
                case "Daily":    return TimeFrame.Daily;
                case "Weekly":   return TimeFrame.Weekly;
                case "Monthly":  return TimeFrame.Monthly;
                default:         return TimeFrame.Hour;
            }
        }

        private static Color GetTfColor(string tfName)
        {
            switch (tfName)
            {
                case "Minute":   return Color.FromArgb(160, 180, 180, 180);
                case "Minute3":  return Color.FromArgb(160, 160, 160, 160);
                case "Minute5":  return Color.FromArgb(160, 140, 140, 140);
                case "Minute15": return Color.FromArgb(160, 120, 120, 120);
                case "Hour":     return Color.FromArgb(230, 255, 165,   0);
                case "Hour4":    return Color.FromArgb(230,  30, 144, 255);
                case "Hour12":   return Color.FromArgb(200, 150, 150, 150);
                case "Daily":    return Color.FromArgb(240, 220,  50,  50);
                case "Weekly":   return Color.FromArgb(240, 240, 240, 240);
                case "Monthly":  return Color.FromArgb(255, 255, 215,   0);
                default:         return Color.FromArgb(180, 200, 200, 200);
            }
        }

        private static int GetTfRank(string tfName)
        {
            switch (tfName)
            {
                case "Minute":   return 0;
                case "Minute3":  return 1;
                case "Minute5":  return 2;
                case "Minute15": return 3;
                case "Hour":     return 4;
                case "Hour4":    return 5;
                case "Hour12":   return 6;
                case "Daily":    return 7;
                case "Weekly":   return 8;
                case "Monthly":  return 9;
                default:         return 4;
            }
        }

        private static double GetTfMinutes(string tfName)
        {
            switch (tfName)
            {
                case "Minute":   return 1;
                case "Minute3":  return 3;
                case "Minute5":  return 5;
                case "Minute15": return 15;
                case "Hour":     return 60;
                case "Hour4":    return 240;
                case "Hour12":   return 720;
                case "Daily":    return 1440;
                case "Weekly":   return 10080;
                case "Monthly":  return 43200;
                default:         return 60;
            }
        }

        private DateTime GetLineEndTime(DateTime origin, string timeframe)
        {
            // A selected S/R level is a ray: the vertical filter decides where
            // it begins, never where it stops.  Shortening old rays made levels
            // disappear before the current price when changing timeframe.
            return _robot.Server.Time.AddDays(365);
        }
    }
}
