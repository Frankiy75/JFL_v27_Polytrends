using System.Collections.Generic;
using System.Linq;
using cAlgo.API;

namespace cAlgo.Robots.JFL_v27_Polytrend
{
    public class ZigZagLineManager
    {
        private readonly Robot _robot;
        private const string Prefix = "ZZ_";
        private readonly Dictionary<string, int> _lastDrawnCounts = new Dictionary<string, int>();

        public ZigZagLineManager(Robot robot)
        {
            _robot = robot;
        }

        public void ClearTimeFrame(string timeframe)
        {
            string tfPrefix = $"{Prefix}{timeframe}_";
            var toRemove = _robot.Chart.Objects.Where(o => o.Name.StartsWith(tfPrefix)).ToList();
            foreach (var obj in toRemove)
                _robot.Chart.RemoveObject(obj.Name);
            
            _lastDrawnCounts[timeframe] = -1;
        }

        public void DrawZigZagFull(JFL_ZigZagService zigzag, string timeframe, Color color, int thickness)
        {
            string tfPrefix = $"{Prefix}{timeframe}_";

            var pivots = zigzag.Pivots;
            if (pivots == null || pivots.Count < 2)
            {
                ClearTimeFrame(timeframe);
                return;
            }

            var allPivots = new List<JFL_Pivot>(pivots);
            var current = zigzag.GetCurrentExtreme();
            if (current != null) allPivots.Add(current);

            int newCount = allPivots.Count - 1;
            
            if (!_lastDrawnCounts.TryGetValue(timeframe, out int lastCount))
                lastCount = -1;

            // Si el número de segmentos se redujo (ej. recálculo), borramos los sobrantes
            if (lastCount > newCount)
            {
                var toRemove = _robot.Chart.Objects
                    .Where(o => o.Name.StartsWith(tfPrefix))
                    .Where(o => { int idx = ParseIndex(o.Name, tfPrefix); return idx >= newCount; })
                    .ToList();
                foreach (var obj in toRemove)
                    _robot.Chart.RemoveObject(obj.Name);
            }

            // Optimizacion: Solo dibujar desde cero si es la primera vez (lastCount == -1)
            // Si ya hemos dibujado, solo actualizamos los ultimos 2 segmentos (el vivo y el recien confirmado)
            int startIdx = (lastCount > 0) ? System.Math.Max(0, newCount - 2) : 0;

            for (int i = startIdx; i < newCount; i++)
            {
                _robot.Chart.DrawTrendLine(
                    $"{tfPrefix}{i}",
                    allPivots[i].Time,     allPivots[i].Price,
                    allPivots[i + 1].Time, allPivots[i + 1].Price,
                    color, thickness, LineStyle.Solid
                );
            }

            _lastDrawnCounts[timeframe] = newCount;
        }

        private static int ParseIndex(string name, string prefix)
        {
            string suffix = name.Substring(prefix.Length);
            return int.TryParse(suffix, out int idx) ? idx : -1;
        }
    }
}
