using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Internals;

namespace cAlgo.Robots.JFL_v27_Polytrend.JFL_v27_Polytrend
{
    public enum PolyBoundsCorner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    [Robot(AccessRights = AccessRights.None)]
    public class JFL_v27_Polytrend : Robot
    {
        // --- PARÁMETROS ---
        [Parameter("Debug Mode", DefaultValue = false)]
        public bool DebugMode { get; set; }

        [Parameter("Show Pair Diagnostics", Group = "Debug", DefaultValue = true)]
        public bool ShowPairDiagnostics { get; set; }

        // --- VISUALS ---
        [Parameter("Support Color", Group = "Visuals", DefaultValue = "Lime")]
        public string SupportColorName { get; set; }

        [Parameter("Resistance Color", Group = "Visuals", DefaultValue = "OrangeRed")]
        public string ResistanceColorName { get; set; }

        // --- SCAN ---
        [Parameter("Enable MTF Scan", Group = "Scan Timeframes", DefaultValue = false)]
        public bool EnableMTFScan { get; set; }

        // --- SCAN (qué TFs se calculan) ---
        [Parameter("Scan M1",  Group = "Scan Timeframes", DefaultValue = false)]
        public bool ScanM1 { get; set; }
        [Parameter("Scan M3",  Group = "Scan Timeframes", DefaultValue = false)]
        public bool ScanM3 { get; set; }
        [Parameter("Scan M5",  Group = "Scan Timeframes", DefaultValue = false)]
        public bool ScanM5 { get; set; }
        [Parameter("Scan M15", Group = "Scan Timeframes", DefaultValue = false)]
        public bool ScanM15 { get; set; }
        [Parameter("Scan H1",  Group = "Scan Timeframes", DefaultValue = false)]
        public bool ScanH1 { get; set; }
        [Parameter("Scan H4",  Group = "Scan Timeframes", DefaultValue = true)]
        public bool ScanH4 { get; set; }
        [Parameter("Scan H12", Group = "Scan Timeframes", DefaultValue = true)]
        public bool ScanH12 { get; set; }
        [Parameter("Scan D",   Group = "Scan Timeframes", DefaultValue = true)]
        public bool ScanD { get; set; }
        [Parameter("Scan W",   Group = "Scan Timeframes", DefaultValue = true)]
        public bool ScanW { get; set; }
        [Parameter("Scan M",   Group = "Scan Timeframes", DefaultValue = true)]
        public bool ScanMn { get; set; }

        // --- LINES (qué TFs se dibujan como líneas S/R) ---
        [Parameter("Lines M1",  Group = "Draw Lines", DefaultValue = false)]
        public bool LinesM1 { get; set; }
        [Parameter("Lines M3",  Group = "Draw Lines", DefaultValue = false)]
        public bool LinesM3 { get; set; }
        [Parameter("Lines M5",  Group = "Draw Lines", DefaultValue = false)]
        public bool LinesM5 { get; set; }
        [Parameter("Lines M15", Group = "Draw Lines", DefaultValue = false)]
        public bool LinesM15 { get; set; }
        [Parameter("Lines H1",  Group = "Draw Lines", DefaultValue = false)]
        public bool LinesH1 { get; set; }
        [Parameter("Lines H4",  Group = "Draw Lines", DefaultValue = false)]
        public bool LinesH4 { get; set; }
        [Parameter("Lines H12", Group = "Draw Lines", DefaultValue = false)]
        public bool LinesH12 { get; set; }
        [Parameter("Lines D",   Group = "Draw Lines", DefaultValue = false)]
        public bool LinesD { get; set; }
        [Parameter("Lines W",   Group = "Draw Lines", DefaultValue = true)]
        public bool LinesW { get; set; }
        [Parameter("Lines M",   Group = "Draw Lines", DefaultValue = true)]
        public bool LinesMn { get; set; }

        [Parameter("Untested Line Width", Group = "Draw Lines", DefaultValue = 1, MinValue = 1, MaxValue = 5)]
        public int UntestedLineWidth { get; set; }

        [Parameter("Tested Line Width", Group = "Draw Lines", DefaultValue = 1, MinValue = 1, MaxValue = 5)]
        public int TestedLineWidth { get; set; }

        // --- FILTRO LÍNEAS (TF del gráfico) ---
        [Parameter("Lines Above Price", Group = "Lines Filter", DefaultValue = 3, MinValue = 0, MaxValue = 50)]
        public int LinesAbove { get; set; }
        [Parameter("Lines Below Price", Group = "Lines Filter", DefaultValue = 3, MinValue = 0, MaxValue = 50)]
        public int LinesBelow { get; set; }

        // --- FILTRO LÍNEAS MTF ---
        [Parameter("MTF Lines Above Price", Group = "Lines Filter", DefaultValue = 3, MinValue = 0, MaxValue = 50)]
        public int MtfLinesAbove { get; set; }
        [Parameter("MTF Lines Below Price", Group = "Lines Filter", DefaultValue = 3, MinValue = 0, MaxValue = 50)]
        public int MtfLinesBelow { get; set; }

        // --- LÍNEA VERTICAL CHART TF (tecla F) ---
        [Parameter("Show Chart TF Line", Group = "Vertical Lines", DefaultValue = true)]
        public bool ShowVertLine { get; set; }

        [Parameter("Chart TF Line Color", Group = "Vertical Lines", DefaultValue = "SkyBlue")]
        public string VertLineColorName { get; set; }

        [Parameter("Chart TF Line Width", Group = "Vertical Lines", DefaultValue = 1, MinValue = 1, MaxValue = 5)]
        public int VertLineWidth { get; set; }

        [Parameter("Chart TF Draw From Date (yyyy-MM-dd)", Group = "Vertical Lines", DefaultValue = "")]
        public string DrawFromDateStr { get; set; }

        // --- LÍNEA VERTICAL MTF (tecla G) ---
        [Parameter("Show MTF Line", Group = "Vertical Lines", DefaultValue = true)]
        public bool ShowMtfVertLine { get; set; }

        [Parameter("MTF Line Color", Group = "Vertical Lines", DefaultValue = "Moccasin")]
        public string MtfVertLineColorName { get; set; }

        [Parameter("MTF Line Width", Group = "Vertical Lines", DefaultValue = 1, MinValue = 1, MaxValue = 5)]
        public int MtfVertLineWidth { get; set; }

        [Parameter("MTF Draw From Date (yyyy-MM-dd)", Group = "Vertical Lines", DefaultValue = "")]
        public string MtfDrawFromDateStr { get; set; }

        [Parameter("F/G Filters Historical Pairs", Group = "Vertical Lines", DefaultValue = true)]
        public bool FilterHistoricalPairsAtVertical { get; set; }

        // --- TESTED LINES ---
        [Parameter("Tested Line Offset (pips)", Group = "Tested Lines", DefaultValue = 0.5, MinValue = 0, MaxValue = 10)]
        public double TestedLineOffsetPips { get; set; }

        [Parameter("Use Wicks for Gains/Losses", Group = "Pair States", DefaultValue = false)]
        public bool UseWicksForGainsLosses { get; set; }

        [Parameter("State Change Offset (pips)", Group = "Pair States", DefaultValue = 0.0, MinValue = 0, MaxValue = 10)]
        public double StateChangeOffsetPips { get; set; }

        [Parameter("Show PolyBounds", Group = "PolyBounds", DefaultValue = true)]
        public bool ShowPolyBounds { get; set; }

        [Parameter("Show Hold Above", Group = "PolyBounds", DefaultValue = true)]
        public bool ShowHoldAbove { get; set; }

        [Parameter("Show Hold Below", Group = "PolyBounds", DefaultValue = true)]
        public bool ShowHoldBelow { get; set; }

        [Parameter("Show ATR", Group = "PolyBounds", DefaultValue = true)]
        public bool ShowPolyBoundsAtr { get; set; }

        [Parameter("ATR Period", Group = "PolyBounds", DefaultValue = 14, MinValue = 2, MaxValue = 200)]
        public int PolyBoundsAtrPeriod { get; set; }

        [Parameter("Corner", Group = "PolyBounds", DefaultValue = PolyBoundsCorner.BottomRight)]
        public PolyBoundsCorner PolyBoundsPosition { get; set; }

        [Parameter("Macro Leg Lookback (bars)", Group = "Macro Structure", DefaultValue = 200, MinValue = 10, MaxValue = 5000)]
        public int MacroLegLookbackBars { get; set; }

        [Parameter("Macro Levels Per Side", Group = "Macro Structure", DefaultValue = 2, MinValue = 0, MaxValue = 20)]
        public int MacroLevelsPerSide { get; set; }

        [Parameter("Minimum Polytrend Leg (ATR)", Group = "Trend Selection", DefaultValue = 1.25, MinValue = 0.10, MaxValue = 20.0)]
        public double MinimumPolytrendLegAtr { get; set; }

        [Parameter("Minimum Retracement (%)", Group = "Trend Selection", DefaultValue = 61.8, MinValue = 10.0, MaxValue = 100.0)]
        public double MinimumPolytrendRetracementPercent { get; set; }

        [Parameter("Recent Pairs Only", Group = "Pair Focus", DefaultValue = false)]
        public bool RecentPairsOnly { get; set; }

        [Parameter("Bright Pairs Per Side", Group = "Pair Focus", DefaultValue = 4, MinValue = 1, MaxValue = 20)]
        public int BrightPairsPerSide { get; set; }

        [Parameter("Faded Pair Transparency", Group = "Pair Focus", DefaultValue = 100, MinValue = 0, MaxValue = 100)]
        public int FadedPairTransparency { get; set; }

        [Parameter("Hide Faded Pairs", Group = "Pair Focus", DefaultValue = false)]
        public bool HideFadedPairs { get; set; }

        [Parameter("Highlight Near Price (ATR)", Group = "Pair Focus", DefaultValue = 0.30, MinValue = 0.01, MaxValue = 5.0)]
        public double HighlightNearPriceAtr { get; set; }

        [Parameter("Thin Stacked Pairs", Group = "Pair Focus", DefaultValue = true)]
        public bool ThinStackedPairs { get; set; }

        [Parameter("Stack Distance (ATR)", Group = "Pair Focus", DefaultValue = 0.25, MinValue = 0.01, MaxValue = 5.0)]
        public double StackDistanceAtr { get; set; }

        [Parameter("Noise Height (ATR)", Group = "Pair Focus", DefaultValue = 0.05, MinValue = 0.0, MaxValue = 5.0)]
        public double NoiseHeightAtr { get; set; }

        [Parameter("Show Only Gained", Group = "Pair Focus", DefaultValue = false)]
        public bool ShowOnlyGained { get; set; }

        [Parameter("Show Only Lost", Group = "Pair Focus", DefaultValue = false)]
        public bool ShowOnlyLost { get; set; }

        [Parameter("Classic Mode", Group = "Pair Focus", DefaultValue = false)]
        public bool ClassicMode { get; set; }

        [Parameter("Old Pair Stub After Bars", Group = "Pair Focus", DefaultValue = 160, MinValue = 0, MaxValue = 5000)]
        public int OldPairStubAfterBars { get; set; }

        [Parameter("Focus Immediate Structure", Group = "Pair Focus", DefaultValue = true)]
        public bool FocusImmediateStructure { get; set; }

        [Parameter("Always Show Recent Pairs", Group = "Pair Focus", DefaultValue = 2, MinValue = 0, MaxValue = 10)]
        public int AlwaysShowRecentPairs { get; set; }

        [Parameter("Max Visible Pairs", Group = "Pair Focus", DefaultValue = 8, MinValue = 1, MaxValue = 20)]
        public int MaxVisiblePairs { get; set; }

        [Parameter("Use Timeframe Profiles", Group = "Timeframe Profiles", DefaultValue = false)]
        public bool UseTimeframeProfiles { get; set; }

        [Parameter("Low TF Pairs Per Side", Group = "Timeframe Profiles", DefaultValue = 4, MinValue = 1, MaxValue = 20)]
        public int LowTfPairsPerSide { get; set; }

        [Parameter("High TF Pairs Per Side", Group = "Timeframe Profiles", DefaultValue = 2, MinValue = 1, MaxValue = 20)]
        public int HighTfPairsPerSide { get; set; }

        [Parameter("Low TF Stack Distance (ATR)", Group = "Timeframe Profiles", DefaultValue = 0.20, MinValue = 0.01, MaxValue = 5)]
        public double LowTfStackDistanceAtr { get; set; }

        [Parameter("High TF Stack Distance (ATR)", Group = "Timeframe Profiles", DefaultValue = 0.08, MinValue = 0.01, MaxValue = 5)]
        public double HighTfStackDistanceAtr { get; set; }

        [Parameter("Show Universal Pattern", Group = "Universal Pattern", DefaultValue = false)]
        public bool ShowUniversalPattern { get; set; }

        [Parameter("Show Prior Patterns", Group = "Universal Pattern", DefaultValue = true)]
        public bool ShowPriorUniversalPatterns { get; set; }

        [Parameter("Pattern Tolerance (ATR)", Group = "Universal Pattern", DefaultValue = 0.20, MinValue = 0.01, MaxValue = 2.0)]
        public double UniversalPatternToleranceAtr { get; set; }

        // --- PATTERN LABELS ---
        [Parameter("Show Pattern Labels", Group = "Pattern Labels", DefaultValue = true)]
        public bool ShowPatternLabels { get; set; }

        [Parameter("High Label Color", Group = "Pattern Labels", DefaultValue = "Red")]
        public string HighLabelColorName { get; set; }

        [Parameter("Low Label Color", Group = "Pattern Labels", DefaultValue = "Lime")]
        public string LowLabelColorName { get; set; }

        [Parameter("Label Font Size", Group = "Pattern Labels", DefaultValue = 9, MinValue = 6, MaxValue = 20)]
        public int LabelFontSize { get; set; }

        [Parameter("Use Candle Body Filter", Group = "Pattern Labels", DefaultValue = true)]
        public bool UseCandleBodyFilter { get; set; }

        // --- SWING EXTREMES ---
        [Parameter("Show Swing Extremes", Group = "Swing Extremes", DefaultValue = true)]
        public bool ShowSwingExtremes { get; set; }

        [Parameter("Swing Screen Mode", Group = "Swing Extremes", DefaultValue = false)]
        public bool SwingScreenMode { get; set; }

        [Parameter("Swing Lookback (bars)", Group = "Swing Extremes", DefaultValue = 100, MinValue = 10, MaxValue = 1000)]
        public int SwingLookback { get; set; }

        [Parameter("Swing High Color", Group = "Swing Extremes", DefaultValue = "OrangeRed")]
        public string SwingHighColorName { get; set; }

        [Parameter("Swing Low Color", Group = "Swing Extremes", DefaultValue = "Lime")]
        public string SwingLowColorName { get; set; }

        // --- ZIGZAG (solo TF del gráfico actual) ---
        [Parameter("Draw ZigZag", Group = "Draw ZigZag", DefaultValue = true)]
        public bool DrawZigZag { get; set; }

        [Parameter("ZigZag Color", Group = "Draw ZigZag", DefaultValue = "Gray")]
        public string ZigZagColorName { get; set; }

        [Parameter("ZigZag Thickness", Group = "Draw ZigZag", DefaultValue = 3, MinValue = 1, MaxValue = 5)]
        public int ZigZagThickness { get; set; }

        [Parameter("ZigZag Transparency (%)", Group = "Draw ZigZag", DefaultValue = 35, MinValue = 0, MaxValue = 90)]
        public int ZigZagTransparency { get; set; }

        // --- COMPONENTES ---
        private MTFZigZagService _mtfZigZag;
        private PolytrendScanner _scanner;
        private PolytrendLineManager _lineManager;
        private ZigZagLineManager _zigZagLineManager;
        private LineTouchDetector _touchDetector;
        private PolytrendStateEngine _stateEngine;
        private PairLifecycleEngine _pairLifecycleEngine;
        private UniversalPatternDetector _universalPatternDetector;
        private UniversalPatternRenderer _universalPatternRenderer;
        private ReversalPatternLabeler _patternLabeler;
        private SwingExtremeMarker _swingMarker;

        private Color _supportColor;
        private Color _resistanceColor;
        private Color _zigZagColor;
        private string _chartTfName;

        private Dictionary<string, bool> _timeframesToScan;
        private Dictionary<string, bool> _linesToDraw;
        private readonly Dictionary<string, HashSet<string>> _stickyPairIds =
            new Dictionary<string, HashSet<string>>();

        private const string VertLineObjName    = "PT_VerticalLine";
        private const string MtfVertLineObjName = "PT_VerticalLine_MTF";
        private const string PlaceModeLabel     = "PT_PlaceModeHint";
        private const string PolyBoundsLabel    = "PT_PolyBounds";
        private const string PairDiagnosticsLabel = "PT_PairDiagnostics";

        private DateTime _vertLineDate;     // filtro Chart TF (tecla F)
        private Color    _vertLineColor;
        private bool     _placingVertLine;  // true mientras esperamos click F

        private DateTime _mtfVertLineDate;  // filtro MTF (tecla G)
        private Color    _mtfVertLineColor;
        private bool     _placingMtfVertLine;

        protected override void OnStart()
        {
            _supportColor    = Opaque(Color.FromName(SupportColorName));
            _resistanceColor = Opaque(Color.FromName(ResistanceColorName));
            _zigZagColor     = WithTransparency(Color.FromName(ZigZagColorName), ZigZagTransparency);
            _vertLineColor   = Color.FromName(VertLineColorName);
            _mtfVertLineColor = Color.FromName(MtfVertLineColorName);
            _chartTfName     = TimeFrame.ToString();

            var dateFormats = new[] { "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy", "yyyy/MM/dd" };

            // --- Línea Chart TF (F) ---
            var existingVl = Chart.Objects.FirstOrDefault(o => o.Name == VertLineObjName) as ChartVerticalLine;
            if (existingVl != null)
            {
                _vertLineDate = existingVl.Time;
            }
            else if (!string.IsNullOrWhiteSpace(DrawFromDateStr) &&
                DateTime.TryParseExact(DrawFromDateStr.Trim(), dateFormats,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var parsedDate))
            {
                _vertLineDate = parsedDate;
            }
            else if (ShowVertLine && Bars.Count > 0)
            {
                int idx = Math.Max(0, Bars.Count - 101);
                _vertLineDate = Bars.OpenTimes[idx];
            }

            // --- Línea MTF (G) ---
            var existingMtfVl = Chart.Objects.FirstOrDefault(o => o.Name == MtfVertLineObjName) as ChartVerticalLine;
            if (existingMtfVl != null)
            {
                _mtfVertLineDate = existingMtfVl.Time;
            }
            else if (!string.IsNullOrWhiteSpace(MtfDrawFromDateStr) &&
                DateTime.TryParseExact(MtfDrawFromDateStr.Trim(), dateFormats,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var parsedMtfDate))
            {
                _mtfVertLineDate = parsedMtfDate;
            }
            else if (ShowMtfVertLine && Bars.Count > 0)
            {
                int idx = Math.Max(0, Bars.Count - 501);
                _mtfVertLineDate = Bars.OpenTimes[idx];
            }

            _mtfZigZag        = new MTFZigZagService(this, DebugMode, Print);
            _scanner          = new PolytrendScanner(MinimumPolytrendLegAtr,
                MinimumPolytrendRetracementPercent);
            _lineManager      = new PolytrendLineManager(this, UntestedLineWidth, TestedLineWidth);
            _lineManager.SetVisualOptions(ClassicMode, OldPairStubAfterBars);
            _zigZagLineManager = new ZigZagLineManager(this);
            _touchDetector    = new LineTouchDetector(Symbol, TestedLineOffsetPips);
            _stateEngine      = new PolytrendStateEngine(Symbol, UseWicksForGainsLosses, StateChangeOffsetPips);
            _pairLifecycleEngine = new PairLifecycleEngine(Symbol, TestedLineOffsetPips);
            _universalPatternDetector = new UniversalPatternDetector();
            _universalPatternRenderer = new UniversalPatternRenderer(this);
            _lineManager.SetTouchDetector(_touchDetector, _mtfZigZag);

            if (ShowPatternLabels)
            {
                _patternLabeler = new ReversalPatternLabeler(Chart, Bars,
                    Color.FromName(HighLabelColorName),
                    Color.FromName(LowLabelColorName),
                    LabelFontSize,
                    UseCandleBodyFilter);
                _patternLabeler.ScanAndLabel(Math.Min(500, Bars.Count));
            }

            if (ShowSwingExtremes)
            {
                _swingMarker = new SwingExtremeMarker(Chart, Bars, SwingLookback, SwingScreenMode,
                    Color.FromName(SwingHighColorName),
                    Color.FromName(SwingLowColorName));
                _swingMarker.Update();
                if (SwingScreenMode)
                {
                    Chart.ZoomChanged += OnChartZoomChanged;
                    Timer.Start(100);
                    Timer.TimerTick += OnSwingTimer;
                }
            }

            _timeframesToScan = new Dictionary<string, bool>
            {
                { "Minute",   ScanM1  },
                { "Minute3",  ScanM3  },
                { "Minute5",  ScanM5  },
                { "Minute15", ScanM15 },
                { "Hour",     ScanH1  },
                { "Hour4",    ScanH4  },
                { "Hour12",   ScanH12 },
                { "Daily",    ScanD   },
                { "Weekly",   ScanW   },
                { "Monthly",  ScanMn  }
            };

            _linesToDraw = new Dictionary<string, bool>
            {
                { "Minute",   LinesM1  },
                { "Minute3",  LinesM3  },
                { "Minute5",  LinesM5  },
                { "Minute15", LinesM15 },
                { "Hour",     LinesH1  },
                { "Hour4",    LinesH4  },
                { "Hour12",   LinesH12 },
                { "Daily",    LinesD   },
                { "Weekly",   LinesW   },
                { "Monthly",  LinesMn  }
            };

            if (EnableMTFScan)
            {
                // Con MTF activo: dibujar todos los TFs que estén escaneados
                foreach (var key in _linesToDraw.Keys.ToList())
                    _linesToDraw[key] = _timeframesToScan.TryGetValue(key, out var scan) && scan;
            }
            else
            {
                // Sin MTF: desactivar todo excepto el TF del gráfico
                foreach (var key in _timeframesToScan.Keys.ToList())
                    _timeframesToScan[key] = false;
                foreach (var key in _linesToDraw.Keys.ToList())
                    _linesToDraw[key] = false;
            }

            // El TF del gráfico siempre se escanea y dibuja
            if (_timeframesToScan.ContainsKey(_chartTfName))
                _timeframesToScan[_chartTfName] = true;
            if (_linesToDraw.ContainsKey(_chartTfName))
                _linesToDraw[_chartTfName] = true;

            _mtfZigZag.OnHistoryLoaded += HandleHistoryLoaded;
            SharedPatternService.OnPatternUpdated += HandleSharedPatternUpdated;
            Chart.ObjectsUpdated += OnChartObjectsUpdated;
            Chart.KeyDown += OnChartKeyDown;
            Chart.MouseDown += OnChartMouseDown;
            Chart.Activated += OnChartActivated;
            Chart.ScrollChanged += OnChartScrollChanged;
            Chart.ZoomChanged += OnChartLabelZoomChanged;

            foreach (var tf in _timeframesToScan.Where(x => x.Value))
                _mtfZigZag.InitializeTimeFrame(ParseTimeFrame(tf.Key));

            DrawVerticalTimeLine();
            Print("JFL v27 Polytrend Initialized - Clean MTF Logic System");
        }

        private TimeFrame ParseTimeFrame(string tfName)
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
                default:         return TimeFrame.Minute;
            }
        }

        private void HandleHistoryLoaded(TimeFrame tf)
        {
            var zz = _mtfZigZag.GetZigZag(tf);
            if (zz != null)
            {
                var scan = _scanner.ScanWithPairs(zz, _mtfZigZag.GetBars(tf), _stateEngine, _pairLifecycleEngine);
                var levels = scan.Levels;
                string tfName = tf.ToString();
                foreach (var l in levels) l.TimeframeName = tfName;
                foreach (var pair in scan.Pairs) pair.TimeframeName = tfName;
                SharedPatternService.RegisterResults(Symbol.Name, tfName, levels, scan.Pairs);
            }
        }

        private void HandleSharedPatternUpdated(string symbol, string tfName)
        {
            BeginInvokeOnMainThread(() =>
            {
                if (symbol != Symbol.Name) return;

                var tf = ParseTimeFrame(tfName);
                var allLevels = SharedPatternService.GetResults(symbol, tfName);

                if (tfName == _chartTfName)
                {
                    if (_linesToDraw.TryGetValue(tfName, out bool drawChart) && drawChart)
                    {
                        var pairs = SharedPatternService.GetPairs(Symbol.Name, tfName);
                        var visible = FilterVisiblePairs(allLevels, pairs, isMtf: false);
                        _lineManager.DrawLevels(visible.Levels, visible.Pairs, tfName, _supportColor, _resistanceColor, _vertLineDate);
                        DrawPairDiagnostics(visible, tfName);
                        DrawPolyBounds(visible.Levels);
                        DrawUniversalPattern(allLevels);
                    }
                    else
                    {
                        _lineManager.ClearTimeFrame(tfName);
                    }
                }
                else
                {
                    RedrawMtfLevels();
                }

                if (DrawZigZag && tfName == _chartTfName)
                {
                    var zz = _mtfZigZag.GetZigZag(tf);
                    if (zz != null)
                        _zigZagLineManager.DrawZigZagFull(zz, tfName, _zigZagColor, ZigZagThickness);
                }
                else if (!DrawZigZag && tfName == _chartTfName)
                {
                    _zigZagLineManager.ClearTimeFrame(tfName);
                }

                if (ShowPatternLabels && tfName == _chartTfName)
                    _patternLabeler?.ScanAndLabel(20);

                if (ShowSwingExtremes && tfName == _chartTfName)
                    _swingMarker?.Update();
            });
        }

        private void DrawVerticalTimeLine()
        {
            Chart.RemoveObject(VertLineObjName);
            if (ShowVertLine && _vertLineDate != DateTime.MinValue)
            {
                var vl = Chart.DrawVerticalLine(VertLineObjName, _vertLineDate, _vertLineColor);
                vl.Thickness     = VertLineWidth;
                vl.LineStyle     = LineStyle.Solid;
                vl.IsInteractive = true;
                vl.IsLocked      = false;
            }

            Chart.RemoveObject(MtfVertLineObjName);
            if (ShowMtfVertLine && _mtfVertLineDate != DateTime.MinValue)
            {
                var vl = Chart.DrawVerticalLine(MtfVertLineObjName, _mtfVertLineDate, _mtfVertLineColor);
                vl.Thickness     = MtfVertLineWidth;
                vl.LineStyle     = LineStyle.Dots;
                vl.IsInteractive = true;
                vl.IsLocked      = false;
            }
        }


        protected override void OnTick()
        {
            // S/R levels only change on bar close — no point rescanning on every tick.
            var closedTfs = _mtfZigZag.UpdateAll();

            if (closedTfs.Count > 0)
            {
                foreach (var tf in closedTfs)
                {
                    var zz = _mtfZigZag.GetZigZag(tf);
                    if (zz != null)
                    {
                        var scan = _scanner.ScanWithPairs(zz, _mtfZigZag.GetBars(tf), _stateEngine, _pairLifecycleEngine);
                        var levels = scan.Levels;
                        string tfName2 = tf.ToString();
                        foreach (var l in levels) l.TimeframeName = tfName2;
                        foreach (var pair in scan.Pairs) pair.TimeframeName = tfName2;
                        SharedPatternService.RegisterResults(Symbol.Name, tfName2, levels, scan.Pairs);
                    }
                }
            }

            // Redraw chart-TF ZigZag on every tick so the live segment tracks current price
            if (DrawZigZag)
            {
                var zzChart = _mtfZigZag.GetZigZag(TimeFrame);
                if (zzChart != null)
                    _zigZagLineManager.DrawZigZagFull(zzChart, _chartTfName, _zigZagColor, ZigZagThickness);
            }

        }

        private static Color Opaque(Color c) => Color.FromArgb(255, c.R, c.G, c.B);

        private static Color WithTransparency(Color color, int transparencyPercent)
        {
            int alpha = 255 * (100 - Math.Max(0, Math.Min(90, transparencyPercent))) / 100;
            return Color.FromArgb(alpha, color.R, color.G, color.B);
        }

        private List<PolytrendResult> FilterLevels(List<PolytrendResult> levels, bool isMtf = false)
        {
            if (levels == null) return new List<PolytrendResult>();

            double currentPrice = Symbol.Bid;
            int aboveCount = isMtf ? MtfLinesAbove : LinesAbove;
            int belowCount = isMtf ? MtfLinesBelow : LinesBelow;
            // The vertical line crops the drawing only. It must not change
            // which structural levels are considered relevant.
            var dated = levels;

            var aboveList = dated
                .Where(l => l.LinePrice > currentPrice)
                .OrderBy(l => l.LinePrice)
                .Take(aboveCount)
                .ToList();

            var belowList = dated
                .Where(l => l.LinePrice <= currentPrice)
                .OrderByDescending(l => l.LinePrice)
                .Take(belowCount)
                .ToList();

            return aboveList.Concat(belowList).ToList();
        }

        private PairVisibility FilterVisiblePairs(List<PolytrendResult> levels,
            List<PolytrendPair> pairs, bool isMtf)
        {
            var visible = new PairVisibility();
            if (levels == null) return visible;
            visible.RawPivotCount = levels.Count;
            DateTime drawFrom = isMtf ? _mtfVertLineDate : _vertLineDate;
            bool applyVerticalFilter = FilterHistoricalPairsAtVertical && drawFrom != DateTime.MinValue;
            var drawableLevels = applyVerticalFilter
                ? levels.Where(level => level.PivotTime >= drawFrom).ToList()
                : levels;
            if (pairs == null || pairs.Count == 0)
            {
                visible.Levels = FilterLevels(drawableLevels, isMtf);
                visible.Diagnostics = "sin pares detectados";
                return visible;
            }

            double price = Symbol.Bid;
            double atr = CalculateAtr();
            var datedPairs = pairs
                .Where(p => p.Support != null && p.Resistance != null)
                // F/G is the beginning of the study: a completed pair to its
                // left cannot leak either level or connector into the chart.
                .Where(p => !applyVerticalFilter || p.EndTime >= drawFrom)
                .ToList();
            visible.RawPairCount = pairs.Count;
            visible.AfterVerticalCount = datedPairs.Count;
            if (ClassicMode)
            {
                foreach (var pair in datedPairs) { pair.IsBright = true; pair.VisualOpacity = 255; }
                visible.Pairs = datedPairs;
                var allIds = new HashSet<string>(datedPairs.SelectMany(p => new[] { p.Support.LevelId, p.Resistance.LevelId }));
                visible.Levels = levels.Where(l => allIds.Contains(l.LevelId)).ToList();
                visible.BasicEligibleCount = datedPairs.Count;
                visible.AfterStackCount = datedPairs.Count;
                visible.BrightPairCount = datedPairs.Count;
                visible.Diagnostics = "modo clásico";
                return visible;
            }
            bool filterByState = ShowOnlyGained || ShowOnlyLost;
            visible.BasicEligibleCount = datedPairs.Count(pair =>
                PairHeight(pair) >= GetPairAtr(pair, atr) * NoiseHeightAtr &&
                (!filterByState ||
                    (ShowOnlyGained && IsGainedPair(pair)) ||
                    (ShowOnlyLost && IsLostPair(pair))));
            datedPairs = ApplyPairFilters(datedPairs, atr);
            visible.AfterStackCount = datedPairs.Count;
            RankPairsByRelevance(datedPairs, price, atr);

            int aboveQuota = GetPairsPerSide(isMtf, true);
            int belowQuota = GetPairsPerSide(isMtf, false);

            var above = datedPairs
                .Where(p => p.Resistance.LinePrice > price)
                .OrderByDescending(p => p.RelevanceScore)
                .Take(aboveQuota);
            var below = datedPairs
                .Where(p => p.Support.LinePrice <= price)
                .OrderByDescending(p => p.RelevanceScore)
                .Take(belowQuota);

            // Tier 1 is made of structural controls, not whatever pair merely
            // happens to be nearest to the current quote.  This lets an older
            // active trend retain its identity as price rotates around it.
            var controls = datedPairs.Where(p => p.IsPolyBoundsReference);
            var currentStructure = datedPairs.Where(p => p.IsTrendOfRelevance ||
                p.IsImmediateStructure || p.IsProgressionContext);
            var required = controls.Concat(currentStructure)
                .GroupBy(p => p.PairId)
                .Select(g => g.First())
                .ToList();

            // Tier 2 fills the remaining slots with confirmed active trends
            // and the best structural levels on either side of price.
            var candidates = above.Concat(below)
                .Concat(datedPairs.Where(p => p.IsTrendActive)
                    .OrderByDescending(p => p.RelevanceScore).Take(MaxVisiblePairs))
                .Concat(datedPairs.Where(p => p.LifecycleState == PairLifecycleState.SupportLostAwaitingRetest ||
                    p.LifecycleState == PairLifecycleState.ResistanceGainedAwaitingRetest))
                .Concat(datedPairs.OrderByDescending(p => p.EndTime).Take(AlwaysShowRecentPairs))
                .GroupBy(p => p.PairId).Select(g => g.First())
                .OrderByDescending(p => p.RelevanceScore)
                .ThenBy(p => PairDistance(p, price))
                .ToList();

            var brightPairs = required
                .OrderByDescending(p => p.RelevanceScore)
                .ThenBy(p => PairDistance(p, price))
                .Take(MaxVisiblePairs)
                .ToList();
            int remainingSlots = Math.Max(0, MaxVisiblePairs - brightPairs.Count);
            brightPairs.AddRange(candidates
                .Where(candidate => !brightPairs.Any(selected => selected.PairId == candidate.PairId))
                .Take(remainingSlots));

            brightPairs = brightPairs
                .OrderByDescending(p => p.RelevanceScore)
                .ThenBy(p => PairDistance(p, price))
                .ToList();

            UpdateStickyPairs(datedPairs, brightPairs);
            foreach (var pair in brightPairs) { pair.IsBright = true; pair.VisualOpacity = 255; }
            visible.BrightPairCount = brightPairs.Count;
            var primaryPair = brightPairs
                .OrderByDescending(p => p.IsTrendOfRelevance)
                .ThenBy(p => PairDistance(p, price))
                .ThenByDescending(p => p.EndTime)
                .FirstOrDefault();
            if (primaryPair != null) primaryPair.IsPrimary = true;
            if (RecentPairsOnly || HideFadedPairs || FadedPairTransparency >= 100)
            {
                visible.Pairs = brightPairs;
            }
            else
            {
                int fadedOpacity = (int)Math.Round(255 * (100 - FadedPairTransparency) / 100.0);
                int fadedSlots = Math.Max(0, MaxVisiblePairs - brightPairs.Count);
                var fadedPairs = datedPairs
                    .Where(p => !brightPairs.Any(b => b.PairId == p.PairId))
                    .OrderByDescending(p => p.RelevanceScore)
                    .ThenBy(p => PairDistance(p, price))
                    .Take(fadedSlots)
                    .ToList();
                foreach (var pair in fadedPairs)
                {
                    pair.IsBright = false;
                    pair.VisualOpacity = fadedOpacity;
                }
                visible.Pairs = brightPairs.Concat(fadedPairs)
                    .OrderBy(p => PairDistance(p, price)).ToList();
            }

            var visibleIds = new HashSet<string>(visible.Pairs
                .SelectMany(p => new[] { p.Support.LevelId, p.Resistance.LevelId }));
            foreach (var macroLevel in GetMacroLevels(datedPairs, price))
                visibleIds.Add(macroLevel.LevelId);
            // A pair that crosses F/G keeps both members. Their historical
            // origin remains cropped by the renderer, but its support or
            // resistance ray remains logically complete from the boundary.
            visible.Levels = levels.Where(l => visibleIds.Contains(l.LevelId)).ToList();

            // A single unpaired pivot is uncommon but should still be rendered.
            if (visible.Levels.Count == 0)
                visible.Levels = FilterLevels(drawableLevels, isMtf);
            visible.Diagnostics = $"pivotes {visible.RawPivotCount} | pares {visible.RawPairCount} | " +
                $"F/G {(applyVerticalFilter ? visible.AfterVerticalCount.ToString() : "guía")} | válidos {visible.BasicEligibleCount} | " +
                $"sin apilar {visible.AfterStackCount} | visibles {visible.BrightPairCount} | líneas {visible.Levels.Count}";
            return visible;
        }

        private List<PolytrendResult> GetMacroLevels(List<PolytrendPair> pairs,
            double price)
        {
            if (pairs == null || MacroLevelsPerSide <= 0 || Bars == null || Bars.Count < 2)
                return new List<PolytrendResult>();

            int lastClosed = Bars.Count - 2;
            int firstMacroBar = Math.Max(0, lastClosed - MacroLegLookbackBars + 1);
            DateTime macroStart = Bars.OpenTimes[firstMacroBar];

            // Macro levels must come from selected trends, never from a raw
            // pivot. An older active trend is retained because it can remain
            // the dominant structure long after the leg itself formed.
            var eligible = pairs
                .Where(p => p.Support != null && p.Resistance != null)
                .Where(p => p.EndTime >= macroStart || p.IsTrendActive || p.IsTrendOfRelevance)
                .SelectMany(p => new[] { p.Support, p.Resistance })
                .GroupBy(level => level.LevelId)
                .Select(group => group.First())
                .ToList();
            var above = eligible.Where(l => l.LinePrice > price)
                .OrderBy(l => l.LinePrice).Take(MacroLevelsPerSide);
            var below = eligible.Where(l => l.LinePrice <= price)
                .OrderByDescending(l => l.LinePrice).Take(MacroLevelsPerSide);
            return above.Concat(below).ToList();
        }

        private List<PolytrendPair> ApplyPairFilters(List<PolytrendPair> pairs, double atr)
        {
            foreach (var pair in pairs)
            {
                pair.IsBright = false;
                pair.IsPrimary = false;
                pair.IsImmediateStructure = false;
                pair.IsPolyBoundsReference = false;
                pair.RelevanceScore = 0;
                pair.VisualOpacity = 255;
                pair.IsStackRepresentative = true;
                pair.IsProgressionContext = false;
            }

            bool filterByState = ShowOnlyGained || ShowOnlyLost;
            var filtered = pairs.Where(p => PairHeight(p) >= GetPairAtr(p, atr) * NoiseHeightAtr)
                .Where(p => !filterByState ||
                    (ShowOnlyGained && IsGainedPair(p)) ||
                    (ShowOnlyLost && IsLostPair(p)))
                // When two pairs occupy the same zone, keep the structurally
                // stronger one. Choosing the newest one here was silently
                // discarding older, more meaningful parent legs.
                .OrderByDescending(p => GetStructuralSeed(p, atr))
                .ThenByDescending(p => p.EndTime)
                .ToList();

            if (!ThinStackedPairs)
                return filtered;

            double stackDistanceAtr = GetStackDistanceAtr();
            var representatives = new List<PolytrendPair>();
            foreach (var pair in filtered)
            {
                var existing = representatives.FirstOrDefault(other =>
                {
                    double distance = Math.Min(GetPairAtr(pair, atr), GetPairAtr(other, atr)) * stackDistanceAtr;
                    // A shared nearby support OR resistance is enough to make
                    // these two pairs one price zone for drawing purposes.
                    return Math.Abs(other.Support.LinePrice - pair.Support.LinePrice) <= distance ||
                        Math.Abs(other.Resistance.LinePrice - pair.Resistance.LinePrice) <= distance;
                });
                if (existing == null)
                {
                    pair.IsStackRepresentative = true;
                    representatives.Add(pair);
                }
                else
                {
                    pair.IsStackRepresentative = false;
                }
            }
            return representatives;
        }

        private bool IsStickyPair(PolytrendPair pair)
        {
            if (pair == null || string.IsNullOrEmpty(pair.PairId) || string.IsNullOrEmpty(pair.TimeframeName))
                return false;
            return _stickyPairIds.TryGetValue(pair.TimeframeName, out var ids) && ids.Contains(pair.PairId);
        }

        private void UpdateStickyPairs(List<PolytrendPair> candidates, List<PolytrendPair> selected)
        {
            if (candidates == null)
                return;

            foreach (var group in candidates.Where(pair => !string.IsNullOrEmpty(pair.TimeframeName))
                .GroupBy(pair => pair.TimeframeName))
            {
                if (!_stickyPairIds.TryGetValue(group.Key, out var ids))
                {
                    ids = new HashSet<string>();
                    _stickyPairIds[group.Key] = ids;
                }
                var validIds = new HashSet<string>(group.Select(pair => pair.PairId));
                ids.RemoveWhere(id => !validIds.Contains(id));
            }

            if (selected == null)
                return;
            foreach (var pair in selected.Where(pair => !string.IsNullOrEmpty(pair.TimeframeName)))
            {
                if (!_stickyPairIds.TryGetValue(pair.TimeframeName, out var ids))
                {
                    ids = new HashSet<string>();
                    _stickyPairIds[pair.TimeframeName] = ids;
                }
                ids.Add(pair.PairId);
            }
        }

        private int GetPairsPerSide(bool isMtf, bool above)
        {
            if (!UseTimeframeProfiles)
                return BrightPairsPerSide;
            if (IsHighTimeframe()) return HighTfPairsPerSide;
            if (IsLowTimeframe()) return LowTfPairsPerSide;
            return isMtf ? (above ? MtfLinesAbove : MtfLinesBelow) : (above ? LinesAbove : LinesBelow);
        }

        private double GetStackDistanceAtr()
        {
            if (!UseTimeframeProfiles) return StackDistanceAtr;
            if (IsHighTimeframe()) return HighTfStackDistanceAtr;
            if (IsLowTimeframe()) return LowTfStackDistanceAtr;
            return StackDistanceAtr;
        }

        private bool IsLowTimeframe()
        {
            string tf = _chartTfName;
            return tf == "Minute" || tf == "Minute3" || tf == "Minute5" || tf == "Minute15";
        }

        private bool IsHighTimeframe()
        {
            string tf = _chartTfName;
            return tf == "Hour4" || tf == "Hour12" || tf == "Daily" || tf == "Weekly" || tf == "Monthly";
        }

        private static double PairHeight(PolytrendPair pair)
        {
            return Math.Abs(pair.Resistance.LinePrice - pair.Support.LinePrice);
        }

        private static double GetStructuralSeed(PolytrendPair pair, double atr)
        {
            double safeAtr = GetPairAtr(pair, atr);
            double displacement = Math.Max(pair.LegStrengthAtr,
                PairHeight(pair) / safeAtr);
            int tests = (pair.Support?.TestCount ?? 0) + (pair.Resistance?.TestCount ?? 0);
            double seed = Math.Min(displacement, 20.0) * 100.0 + Math.Min(tests, 8) * 8.0;
            if (pair.IsTrendActive) seed += 35.0;
            return seed;
        }

        private static double GetPairAtr(PolytrendPair pair, double fallbackAtr)
        {
            if (pair != null && pair.SourceAtr > 0)
                return pair.SourceAtr;
            return fallbackAtr > 0 ? fallbackAtr : 1.0;
        }

        private static bool IsGainedPair(PolytrendPair pair)
        {
            return pair.Support.State == PolytrendLevelState.SupportGained ||
                pair.Support.State == PolytrendLevelState.Gained ||
                pair.Resistance.State == PolytrendLevelState.SupportGained ||
                pair.Resistance.State == PolytrendLevelState.Gained;
        }

        private static bool IsLostPair(PolytrendPair pair)
        {
            return pair.Support.State == PolytrendLevelState.Lost ||
                pair.Support.State == PolytrendLevelState.ResistanceLost ||
                pair.Resistance.State == PolytrendLevelState.Lost ||
                pair.Resistance.State == PolytrendLevelState.ResistanceLost;
        }

        private void RankPairsByRelevance(List<PolytrendPair> pairs, double price, double atr)
        {
            if (pairs == null || pairs.Count == 0) return;
            double safeAtr = atr > 0 ? atr : Symbol.PipSize * 100;
            DateTime latestEnd = pairs.Max(p => p.EndTime);
            var supportReference = pairs
                .Where(p => p.Support.State == PolytrendLevelState.SupportGained && p.Support.LinePrice <= price)
                .OrderByDescending(p => p.Support.LinePrice).FirstOrDefault();
            var resistanceReference = pairs
                .Where(p => p.Resistance.State == PolytrendLevelState.ResistanceLost && p.Resistance.LinePrice >= price)
                .OrderBy(p => p.Resistance.LinePrice).FirstOrDefault();

            PolytrendPair activeBullish = null;
            PolytrendPair activeBearish = null;
            if (FocusImmediateStructure)
            {
                activeBullish = pairs
                    .Where(p => p.IsBullishMove && price < p.Resistance.LinePrice)
                    .OrderByDescending(p => p.EndTime).FirstOrDefault();
                activeBearish = pairs
                    .Where(p => !p.IsBullishMove && price > p.Support.LinePrice)
                    .OrderByDescending(p => p.EndTime).FirstOrDefault();
            }

            foreach (var pair in pairs)
            {
                double pairAtr = GetPairAtr(pair, safeAtr);
                double distanceAtr = PairDistance(pair, price) / pairAtr;
                double heightAtr = PairHeight(pair) / pairAtr;
                double hoursAgo = Math.Max(0, (Bars.OpenTimes.LastValue - pair.EndTime).TotalHours);

                pair.IsImmediateStructure = pair == activeBullish || pair == activeBearish;
                pair.IsPolyBoundsReference = pair == supportReference || pair == resistanceReference;
                // First rank the leg itself. Proximity is deliberately a
                // small final adjustment: a nearby internal swing is not more
                // important than the major leg that created the structure.
                double structuralAtr = Math.Max(pair.LegStrengthAtr, heightAtr);
                int tests = pair.Support.TestCount + pair.Resistance.TestCount;
                bool isDefendedRange = pair.Support.State == PolytrendLevelState.SupportGained &&
                    pair.Resistance.State == PolytrendLevelState.ResistanceLost;
                double score = Math.Min(structuralAtr, 20.0) * 16.0;
                score += Math.Min(tests, 8) * 7.0;
                if (pair.Support.IsTested && pair.Resistance.IsTested) score += 15.0;
                if (pair.IsTrendActive) score += 35.0;
                if (isDefendedRange) score += 25.0;
                score += 12.0 / (1.0 + distanceAtr);
                score += Math.Max(0, 5.0 - hoursAgo / 24.0);
                if (pair.LifecycleState == PairLifecycleState.SupportLostAwaitingRetest ||
                    pair.LifecycleState == PairLifecycleState.ResistanceGainedAwaitingRetest)
                    score += 30.0;
                else if (pair.LifecycleState == PairLifecycleState.SupportLostRetested ||
                         pair.LifecycleState == PairLifecycleState.ResistanceGainedRetested)
                    score += Math.Max(2.0, 12.0 - (pair.ExpectedRetestCount - 1) * 3.0);
                if (pair.IsPolyBoundsReference) score += 18.0;
                if (pair.IsImmediateStructure) score += 12.0;
                if (pair.IsTrendOfRelevance) score += 8.0;
                // A selected pair gets a small persistence preference on the
                // next redraw. It can still be replaced by a clearly stronger
                // structure, but will not flicker in and out on minor changes.
                if (IsStickyPair(pair)) score += 20.0;
                if (pair.EndTime == latestEnd) score += 3.0;
                pair.RelevanceScore = score;
            }

            // Keep the preceding step of a fresh pair available as context. It
            // documents the progression without imposing the discarded
            // direction-only rule on the selection.
            var freshPreviousIds = new HashSet<string>(pairs
                .Where(p => p.LifecycleState == PairLifecycleState.SupportLostAwaitingRetest ||
                            p.LifecycleState == PairLifecycleState.ResistanceGainedAwaitingRetest)
                .Select(p => p.PreviousPairId)
                .Where(id => !string.IsNullOrEmpty(id)));
            foreach (var pair in pairs.Where(p => freshPreviousIds.Contains(p.PairId)))
            {
                pair.IsProgressionContext = true;
                pair.RelevanceScore += 12.0;
            }
        }


        private static double PairDistance(PolytrendPair pair, double price)
        {
            if (price < pair.Support.LinePrice) return pair.Support.LinePrice - price;
            if (price > pair.Resistance.LinePrice) return price - pair.Resistance.LinePrice;
            return 0;
        }

        private void OnChartObjectsUpdated(ChartObjectsUpdatedEventArgs args)
        {
            foreach (var obj in args.ChartObjects)
            {
                if (obj is ChartVerticalLine vl)
                {
                    if (obj.Name == VertLineObjName)
                    {
                        _vertLineDate = vl.Time;
                        RedrawAllLevels();
                    }
                    else if (obj.Name == MtfVertLineObjName)
                    {
                        _mtfVertLineDate = vl.Time;
                        RedrawAllLevels();
                    }
                }
            }
        }

        private void RedrawAllLevels()
        {
            if (_linesToDraw.TryGetValue(_chartTfName, out var drawChart) && drawChart)
            {
                var levels = SharedPatternService.GetResults(Symbol.Name, _chartTfName);
                if (levels != null)
                {
                    var pairs = SharedPatternService.GetPairs(Symbol.Name, _chartTfName);
                    var visible = FilterVisiblePairs(levels, pairs, isMtf: false);
                    _lineManager.DrawLevels(visible.Levels, visible.Pairs, _chartTfName, _supportColor, _resistanceColor, _vertLineDate);
                    DrawPairDiagnostics(visible, _chartTfName);
                    DrawPolyBounds(visible.Levels);
                    DrawUniversalPattern(levels);
                }
            }
            RedrawMtfLevels();
        }

        private void RedrawMtfLevels()
        {
            if (!EnableMTFScan) return;

            var mtfNames = _linesToDraw
                .Where(kv => kv.Value && kv.Key != _chartTfName)
                .Select(kv => kv.Key)
                .ToList();

            var pool = new List<PolytrendResult>();
            var pairPool = new List<PolytrendPair>();
            foreach (var tfName in mtfNames)
            {
                var levels = SharedPatternService.GetResults(Symbol.Name, tfName);
                if (levels != null)
                    pool.AddRange(levels.Where(l => l.TimeframeName != _chartTfName));
                var pairs = SharedPatternService.GetPairs(Symbol.Name, tfName);
                if (pairs != null)
                    pairPool.AddRange(pairs.Where(p => p.TimeframeName != _chartTfName));
            }

            var visible = FilterVisiblePairs(pool, pairPool, isMtf: true);
            _lineManager.DrawMtfPool(visible.Levels, visible.Pairs, mtfNames,
                _supportColor, _resistanceColor, _chartTfName, _mtfVertLineDate);
        }

        private void OnChartActivated(ChartActivationChangedEventArgs args)
        {
            DrawVerticalTimeLine();
            RedrawAllLevels();
            if (DrawZigZag)
            {
                var chartTf = ParseTimeFrame(_chartTfName);
                var zz = _mtfZigZag.GetZigZag(chartTf);
                if (zz != null)
                    _zigZagLineManager.DrawZigZagFull(zz, _chartTfName, _zigZagColor, ZigZagThickness);
            }
            if (ShowPatternLabels)
                _patternLabeler?.ScanAndLabel(Math.Min(500, Bars.Count));
            if (ShowSwingExtremes)
                _swingMarker?.Update();
        }

        private void OnChartZoomChanged(ChartZoomEventArgs args)
        {
            _swingMarker?.Update();
        }

        private void OnChartLabelZoomChanged(ChartZoomEventArgs args)
        {
            RedrawAllLevels();
        }

        private void OnChartScrollChanged(ChartScrollEventArgs args)
        {
            RedrawAllLevels();
        }

        private void OnSwingTimer()
        {
            _swingMarker?.Update();
        }

        private void OnChartKeyDown(ChartKeyboardEventArgs args)
        {
            if (args.Key == Key.F)
            {
                _placingMtfVertLine = false;
                _placingVertLine = !_placingVertLine;
                UpdatePlaceModeHint();
            }
            else if (args.Key == Key.G)
            {
                _placingVertLine = false;
                _placingMtfVertLine = !_placingMtfVertLine;
                UpdatePlaceModeHint();
            }
        }

        private void OnChartMouseDown(ChartMouseEventArgs args)
        {
            if (_placingVertLine)
            {
                _vertLineDate = args.TimeValue;
                _placingVertLine = false;
                UpdatePlaceModeHint();
                DrawVerticalTimeLine();
                RedrawAllLevels();
            }
            else if (_placingMtfVertLine)
            {
                _mtfVertLineDate = args.TimeValue;
                _placingMtfVertLine = false;
                UpdatePlaceModeHint();
                DrawVerticalTimeLine();
                RedrawAllLevels();
            }
        }

        private void UpdatePlaceModeHint()
        {
            Chart.RemoveObject(PlaceModeLabel);
            if (_placingVertLine)
            {
                Chart.DrawStaticText(PlaceModeLabel, "[ F ] Click para colocar línea Chart TF",
                    VerticalAlignment.Bottom, HorizontalAlignment.Center, _vertLineColor).FontSize = 10;
            }
            else if (_placingMtfVertLine)
            {
                Chart.DrawStaticText(PlaceModeLabel, "[ G ] Click para colocar línea MTF",
                    VerticalAlignment.Bottom, HorizontalAlignment.Center, _mtfVertLineColor).FontSize = 10;
            }
        }

        private void DrawPairDiagnostics(PairVisibility visible, string timeframe)
        {
            Chart.RemoveObject(PairDiagnosticsLabel);
            if (!ShowPairDiagnostics || visible == null)
                return;

            string details = string.IsNullOrWhiteSpace(visible.Diagnostics)
                ? $"pivotes {visible.RawPivotCount} | pares {visible.RawPairCount}"
                : visible.Diagnostics;
            var text = Chart.DrawStaticText(PairDiagnosticsLabel,
                $"PAIR DIAGNOSTICS [{TfUtils.GetLabel(timeframe)}]{Environment.NewLine}{details}",
                VerticalAlignment.Top, HorizontalAlignment.Left, Color.LightGray);
            text.FontSize = 10;
        }

        private void DrawPolyBounds(List<PolytrendResult> visibleLevels)
        {
            Chart.RemoveObject(PolyBoundsLabel);
            if (!ShowPolyBounds || visibleLevels == null) return;

            double price = Symbol.Bid;
            var support = visibleLevels
                .Where(l => l.State == PolytrendLevelState.SupportGained && l.LinePrice <= price)
                .OrderByDescending(l => l.LinePrice)
                .FirstOrDefault();
            var resistance = visibleLevels
                .Where(l => l.State == PolytrendLevelState.ResistanceLost && l.LinePrice >= price)
                .OrderBy(l => l.LinePrice)
                .FirstOrDefault();

            var lines = new List<string> { "PolyBounds" };
            if (ShowHoldAbove && support != null)
                lines.Add($"▲ hold above: SG {support.LinePrice.ToString("F" + Symbol.Digits)}");
            if (ShowHoldBelow && resistance != null)
                lines.Add($"▼ hold below: RL {resistance.LinePrice.ToString("F" + Symbol.Digits)}");
            if (ShowPolyBoundsAtr)
                lines.Add("ATR: " + CalculateAtr().ToString("F" + Symbol.Digits));

            GetPolyBoundsAlignment(out var vertical, out var horizontal);
            Chart.DrawStaticText(PolyBoundsLabel, string.Join(Environment.NewLine, lines),
                vertical, horizontal, Color.LightGray).FontSize = 10;
        }

        private void DrawUniversalPattern(List<PolytrendResult> levels)
        {
            if (!ShowUniversalPattern)
            {
                _universalPatternRenderer?.Draw(null, false);
                return;
            }
            double tolerance = CalculateAtr() * UniversalPatternToleranceAtr;
            var patterns = _universalPatternDetector?.Detect(levels, tolerance, 4);
            _universalPatternRenderer?.Draw(patterns, ShowPriorUniversalPatterns);
        }

        private double CalculateAtr()
        {
            int lastClosed = Bars.Count - 2;
            if (lastClosed < 1) return 0;

            int first = Math.Max(1, lastClosed - PolyBoundsAtrPeriod + 1);
            double sum = 0;
            int count = 0;
            for (int i = first; i <= lastClosed; i++)
            {
                double previousClose = Bars.ClosePrices[i - 1];
                double range = Math.Max(Bars.HighPrices[i] - Bars.LowPrices[i],
                    Math.Max(Math.Abs(Bars.HighPrices[i] - previousClose), Math.Abs(Bars.LowPrices[i] - previousClose)));
                sum += range;
                count++;
            }
            return count == 0 ? 0 : sum / count;
        }

        private void GetPolyBoundsAlignment(out VerticalAlignment vertical, out HorizontalAlignment horizontal)
        {
            vertical = PolyBoundsPosition == PolyBoundsCorner.TopLeft || PolyBoundsPosition == PolyBoundsCorner.TopRight
                ? VerticalAlignment.Top : VerticalAlignment.Bottom;
            horizontal = PolyBoundsPosition == PolyBoundsCorner.TopLeft || PolyBoundsPosition == PolyBoundsCorner.BottomLeft
                ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        }

        private class PairVisibility
        {
            public List<PolytrendResult> Levels { get; set; } = new List<PolytrendResult>();
            public List<PolytrendPair> Pairs { get; set; } = new List<PolytrendPair>();
            public int RawPivotCount { get; set; }
            public int RawPairCount { get; set; }
            public int AfterVerticalCount { get; set; }
            public int BasicEligibleCount { get; set; }
            public int AfterStackCount { get; set; }
            public int BrightPairCount { get; set; }
            public string Diagnostics { get; set; }
        }

        protected override void OnStop()
        {
            Chart.ObjectsUpdated -= OnChartObjectsUpdated;
            Chart.KeyDown -= OnChartKeyDown;
            Chart.MouseDown -= OnChartMouseDown;
            Chart.Activated -= OnChartActivated;
            Chart.ScrollChanged -= OnChartScrollChanged;
            Chart.ZoomChanged -= OnChartLabelZoomChanged;
            if (SwingScreenMode)
            {
                Chart.ZoomChanged -= OnChartZoomChanged;
                Timer.TimerTick -= OnSwingTimer;
                Timer.Stop();
            }
            Chart.RemoveObject(PlaceModeLabel);
            _patternLabeler?.Clear();
            _swingMarker?.Clear();
            _mtfZigZag.Disconnect();
            SharedPatternService.OnPatternUpdated -= HandleSharedPatternUpdated;
            
            // Conservamos ambas líneas verticales al detener (cambio de TF) para que OnStart las recupere
            var toRemove = Chart.Objects.Where(o => o.Name.StartsWith("PT_")
                && o.Name != VertLineObjName && o.Name != MtfVertLineObjName).ToList();
            foreach (var obj in toRemove)
            {
                Chart.RemoveObject(obj.Name);
            }
        }
    }
}
