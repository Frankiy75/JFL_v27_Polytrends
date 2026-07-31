# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Deploy

This is a **cTrader cBot** (algorithmic trading robot) built with the `cTrader.Automate` SDK targeting `.NET 6`. It is not run from the command line — it compiles and runs inside the cTrader platform.

- **Build**: `dotnet build` from the terminal to check for compile errors. Or Ctrl+B inside the cTrader IDE.
- **Run**: The robot is attached to a chart in cTrader. Parameters are set through the cTrader UI dialog.
- **Debug output**: Use `Print()` calls; they appear in the cTrader Log panel. `DebugMode` parameter gates verbose logging.

## Architecture

The bot detects support/resistance levels across multiple timeframes using a ZigZag-based approach and draws horizontal lines and ZigZag segments on the chart.

### Data Flow

```
OnTick() → MTFZigZagService.UpdateAll()
         → [if bar closed] PolytrendScanner.Scan(zigzag) per TF
         → SharedPatternService.RegisterResults(symbol, tf, levels)
         → SharedPatternService.OnPatternUpdated event fires
         → HandleSharedPatternUpdated() → FilterLevels() → PolytrendLineManager.DrawLevels()
         → [every tick] ZigZagLineManager.DrawZigZagFull() for chart TF (live segment tracking)
```

On startup, `OnHistoryLoaded` fires per timeframe as background history loads, triggering the same scan/draw pipeline. `OnChartActivated` redraws everything when the user switches back to the chart.

### Key Components

**`JFL_v27_Polytrend.cs`** — Entry point (`Robot` subclass). Wires all services together. Key responsibilities:
- Manages `_vertLineDate` / `_mtfVertLineDate` (date filter via vertical line object on chart, key F).
- `FilterLevels()` — keeps N closest levels above/below current price, respects cutoff date.
- Draws MTF pool: all non-chart-TF levels merged, filtered, drawn via `DrawMtfPool()`.

**`Core/MTFZigZagService.cs`** — Orchestrates one `JFL_ZigZagService` per enabled timeframe. Calls `GetBars()` + `LoadMoreHistory()` to reach 2000 bars target. Rate-limits `UpdateAll()` to ≥100ms intervals. Uses `BeginInvokeOnMainThread` for async history callbacks. Must call `Disconnect()` on stop to unsubscribe `bars.HistoryLoaded` events. Exposes `GetBars(TimeFrame)` for per-TF bars access.

**`Core/ZigZagService.cs`** (`JFL_ZigZagService`) — Core algorithm. Uses MQL5-style save/restore pattern: after closed bars are processed, state is saved (`SaveState()`); on each tick, state is restored (`RestoreState()`) before processing bar 0, preventing live-tick pivots from becoming permanent. Pivot cap: 1000 pivots max. Adaptive reversal threshold: `max(avgRange * 0.05, currentBarRange * 0.10)`. `GetCurrentExtreme()` returns the live (unconfirmed) pivot for real-time ZigZag rendering.

**`Core/SharedPatternService.cs`** — Static singleton (in-process shared state). Stores scan results keyed by `symbol → tfName → List<PolytrendResult>`. Also manages a blacklist (for invalidated levels, FIFO-capped at 1000 entries) and a master chart registry. All three dictionaries use separate locks.

**`Core/Pivot.cs`** (`JFL_Pivot`) — Plain data class: price, time, isHigh, closePrice, confirmed.

**`Logic/PolytrendScanner.cs`** — Converts `JFL_ZigZagService` pivots to `PolytrendResult` objects. `LinePrice` = pivot's close price (body-based level); `PivotPrice` = actual high/low wick.

**`Logic/PolytrendLineManager.cs`** — Draws/redraws horizontal lines on the chart. Two draw paths:
- `DrawLevels()` — chart TF levels (solid = untested, dashed = wick-tested).
- `DrawMtfPool()` — MTF levels from higher TFs (dotted = untested, dashed = wick-tested).
- Each level gets a short tick mark at the pivot origin and a TF label offset to the right.
- Uses `LineTouchDetector.IsWickTested()` with the correct per-TF Bars object (MTF levels use `_mtfZigZag.GetBars()`).

**`Logic/LineTouchDetector.cs`** — Detects whether the last bar crossing a price level does so only via wick. Searches backward from the last closed bar to the pivot origin bar (`pivotTime` param). `offsetPips` widens the crossing band. Returns `true` = wick-only (tested), `false` = body crossed or no crossing found.

**`Logic/ZigZagLineManager.cs`** — Draws diagonal ZigZag segments between confirmed pivots + current live extreme. Does incremental redraw (removes only segments beyond the new count). Called on every tick for chart TF to keep the live segment current.

**`Logic/ReversalPatternLabeler.cs`** — 5-bar peak/valley detection drawn as text labels at pivot highs/lows. Peak: `H[-2] < H[-1] ≤ H[0] ≥ H[+1] > H[+2]`. Valley: inverse on lows. Label text = TF short name (e.g. "D", "1H"). Caches drawn labels; `CleanupInvalidated()` removes labels that no longer qualify on each scan. Prefix: `PT_RevLbl_{tfLabel}_`.

**`Logic/PolytrendStructures.cs`** — `PolytrendResult` DTO: `IsResistance`, `PivotTime`, `PivotPrice`, `LinePrice`, `TimeframeName`.

### Tested vs Untested Line Styles

| Context | Untested | Tested (wick-only touch) |
|---|---|---|
| Chart TF | `LineStyle.Solid` | `LineStyle.Lines` (dashed) |
| MTF | `LineStyle.Dots` | `LineStyle.Lines` (dashed) |

### Threading Notes

- `MTFZigZagService` uses a single `_syncLock` for all its dictionaries.
- `SharedPatternService` uses separate locks per data structure (`_store`, `_blacklist`, `_masterRegistry`).
- History-loaded callbacks use `BeginInvokeOnMainThread` — all chart drawing must happen on the main thread.
- `_isDisconnected` flag is `volatile` to signal the async handler safely.

### ZigZag Direction Convention

- `_currentDirection == "up"` → tracking a HIGH extreme (next pivot will be a low)
- `_currentDirection == "down"` → tracking a LOW extreme (next pivot will be a high)

This is inverted from the visual: "direction up" means the market was going up and we're tracking the top of that move.
