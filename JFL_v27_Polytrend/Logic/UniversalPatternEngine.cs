using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;

namespace cAlgo.Robots.JFL_v27_Polytrend
{
    public class UniversalPattern
    {
        public List<PolytrendResult> Points { get; set; }
        public double BottomPrice { get; set; }
        public double TopPrice { get; set; }
    }

    /// <summary>
    /// Finds a complete low-high-low-high-low-high sequence where the first two
    /// lows form a double bottom and the final two highs form a double top.
    /// Tolerance is supplied by the caller in price units (normally ATR based).
    /// </summary>
    public class UniversalPatternDetector
    {
        public List<UniversalPattern> Detect(List<PolytrendResult> levels, double tolerance, int maximum)
        {
            var found = new List<UniversalPattern>();
            if (levels == null || levels.Count < 6) return found;

            var points = levels.OrderBy(l => l.PivotTime).ToList();
            for (int i = 0; i <= points.Count - 6; i++)
            {
                var p = points.Skip(i).Take(6).ToList();
                if (p[0].IsResistance || !p[1].IsResistance || p[2].IsResistance ||
                    !p[3].IsResistance || p[4].IsResistance || !p[5].IsResistance)
                    continue;

                bool doubleBottom = Math.Abs(p[0].LinePrice - p[2].LinePrice) <= tolerance;
                bool doubleTop = Math.Abs(p[3].LinePrice - p[5].LinePrice) <= tolerance;
                if (!doubleBottom || !doubleTop) continue;

                found.Add(new UniversalPattern
                {
                    Points = p,
                    BottomPrice = (p[0].LinePrice + p[2].LinePrice) / 2.0,
                    TopPrice = (p[3].LinePrice + p[5].LinePrice) / 2.0
                });
            }
            return found.OrderByDescending(p => p.Points.Last().PivotTime).Take(maximum).ToList();
        }
    }

    public class UniversalPatternRenderer
    {
        private const string Prefix = "PT_UP_";
        private readonly Robot _robot;

        public UniversalPatternRenderer(Robot robot) { _robot = robot; }

        public void Draw(List<UniversalPattern> patterns, bool showPrior)
        {
            foreach (var obj in _robot.Chart.Objects.Where(o => o.Name.StartsWith(Prefix)).ToList())
                _robot.Chart.RemoveObject(obj.Name);
            if (patterns == null || patterns.Count == 0) return;

            int count = showPrior ? Math.Min(4, patterns.Count) : 1;
            for (int n = 0; n < count; n++)
            {
                var pattern = patterns[n];
                Color color = n == 0 ? Color.FromArgb(220, 255, 215, 0) : Color.FromArgb(100, 150, 150, 150);
                string name = Prefix + n;
                for (int i = 0; i < pattern.Points.Count - 1; i++)
                {
                    _robot.Chart.DrawTrendLine(name + "_sk_" + i,
                        pattern.Points[i].PivotTime, pattern.Points[i].PivotPrice,
                        pattern.Points[i + 1].PivotTime, pattern.Points[i + 1].PivotPrice,
                        color, n == 0 ? 2 : 1, LineStyle.Solid);
                }
                DateTime start = pattern.Points.First().PivotTime;
                DateTime end = _robot.Server.Time.AddDays(365);
                _robot.Chart.DrawTrendLine(name + "_bottom", start, pattern.BottomPrice, end, pattern.BottomPrice,
                    color, 1, LineStyle.Lines);
                _robot.Chart.DrawTrendLine(name + "_top", start, pattern.TopPrice, end, pattern.TopPrice,
                    color, 1, LineStyle.Lines);
            }
        }
    }
}
