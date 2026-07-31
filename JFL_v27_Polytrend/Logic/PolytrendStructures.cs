using System;
using System.Collections.Generic;

namespace cAlgo.Robots.JFL_v27_Polytrend
{
    public enum LevelRole
    {
        Support,
        Resistance
    }

    /// <summary>
    /// The most recent structural meaning of a level.  A support below price is SG,
    /// a resistance above price is RL; a level crossed to the opposite side is G or L.
    /// </summary>
    public enum PolytrendLevelState
    {
        Gained,
        Lost,
        SupportGained,
        ResistanceLost
    }

    /// <summary>
    /// The live sequence of a pair after one of its two levels changes role.
    /// This makes the first revisit of the opposite member explicit instead of
    /// treating every historical touch as equally relevant.
    /// </summary>
    public enum PairLifecycleState
    {
        Neutral,
        SupportLostAwaitingRetest,
        SupportLostRetested,
        ResistanceGainedAwaitingRetest,
        ResistanceGainedRetested
    }

    public class PolytrendResult
    {
        public string LevelId { get; set; }
        public bool IsValid { get; set; }
        public string Type { get; set; } // "SUPPORT" or "RESISTANCE"
        public bool IsResistance { get; set; }
        public DateTime PivotTime { get; set; }
        public double PivotPrice { get; set; }
        public double LinePrice { get; set; }
        public LevelRole Role { get; set; }
        public string TimeframeName { get; set; }
        public List<string> PairIds { get; set; } = new List<string>();
        public PolytrendLevelState State { get; set; }
        public DateTime StateChangedAt { get; set; }
        public bool IsTested { get; set; }
        public DateTime? LastTestTime { get; set; }
        public int TestCount { get; set; }
    }

    /// <summary>
    /// The two body levels created by one confirmed ZigZag move.  Pairs are kept
    /// independently from the level list because a pivot can participate in both
    /// the incoming and outgoing moves.
    /// </summary>
    public class PolytrendPair
    {
        public string PairId { get; set; }
        public string TimeframeName { get; set; }
        public PolytrendResult Support { get; set; }
        public PolytrendResult Resistance { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsBullishMove { get; set; }
        public bool IsBright { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsImmediateStructure { get; set; }
        public bool IsPolyBoundsReference { get; set; }
        public double RelevanceScore { get; set; }
        public int VisualOpacity { get; set; } = 255;
        public bool IsStackRepresentative { get; set; } = true;
        public PairLifecycleState LifecycleState { get; set; }
        public string TriggerLevelId { get; set; }
        public string ExpectedRetestLevelId { get; set; }
        public DateTime LifecycleStartedAt { get; set; }
        public int ExpectedRetestCount { get; set; }
        public DateTime? LastExpectedRetestTime { get; set; }
        public string PreviousPairId { get; set; }
        public string NextPairId { get; set; }
        public bool IsProgressionContext { get; set; }
        // A bullish trend is invalidated when its origin support is lost; a
        // bearish trend is invalidated when its origin resistance is regained.
        public bool IsTrendActive { get; set; }
        public bool IsTrendInvalidated { get; set; }
        public DateTime? TrendInvalidatedAt { get; set; }
        public bool IsTrendOfRelevance { get; set; }
        // Several adjacent pairs can form the one higher-order leg currently
        // driving price. They are drawn as a single dotted ZigZag path.
        public bool IsMainTrendSegment { get; set; }
        public double LegStrengthAtr { get; set; }
        // ATR measured on the timeframe that created this pair. Rendering on
        // another chart timeframe must not change its structural scale.
        public double SourceAtr { get; set; }
    }

    public class PolytrendScanResult
    {
        public List<PolytrendResult> Levels { get; } = new List<PolytrendResult>();
        public List<PolytrendPair> Pairs { get; } = new List<PolytrendPair>();
    }
}
