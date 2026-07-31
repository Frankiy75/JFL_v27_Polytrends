using cAlgo.API;

namespace cAlgo.Robots.JFL_v27_Polytrend
{
    internal static class TfUtils
    {
        internal static string GetLabel(TimeFrame tf)
        {
            if (tf == TimeFrame.Minute)   return "1m";
            if (tf == TimeFrame.Minute3)  return "3m";
            if (tf == TimeFrame.Minute5)  return "5m";
            if (tf == TimeFrame.Minute15) return "15m";
            if (tf == TimeFrame.Hour)     return "1H";
            if (tf == TimeFrame.Hour4)    return "4H";
            if (tf == TimeFrame.Hour12)   return "12H";
            if (tf == TimeFrame.Daily)    return "D";
            if (tf == TimeFrame.Weekly)   return "W";
            if (tf == TimeFrame.Monthly)  return "M";
            return tf.ToString();
        }

        internal static string GetLabel(string tfName)
        {
            switch (tfName)
            {
                case "Minute":   return "1m";
                case "Minute3":  return "3m";
                case "Minute5":  return "5m";
                case "Minute15": return "15m";
                case "Hour":     return "1H";
                case "Hour4":    return "4H";
                case "Hour12":   return "12H";
                case "Daily":    return "D";
                case "Weekly":   return "W";
                case "Monthly":  return "M";
                default:         return tfName;
            }
        }
    }
}
