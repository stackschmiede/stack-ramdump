# ram-dump

## Stand
- 2026-04-21 Session 4/5: RAM-Tab Redesign + Monitor Auto-Höhe + Splash-Animation
- Status: bereit zum Windows-Test — git pull → dotnet run (Admin)
- Build: 0 Warnungen, 0 Fehler (Windows-dotnet via WSL)

## Struktur
- .NET 8 WPF, MVVM (CommunityToolkit.Mvvm)
- NuGet: CommunityToolkit.Mvvm 8.4.0, Hardcodet.NotifyIcon.Wpf 2.0.1, LibreHardwareMonitorLib 0.9.4

## Session 4 — Was gebaut wurde
- Phase 1: ProcessClassifier + Category/Tag im VM (Browser/Dev/System/Other)
- Phase 2: ActiveFilter (all|top|browser|dev|sys|crit), Multi-Select, SelectedProcesses, TrimSelected/KillSelected, 6 Counts (All/Top/Browser/Dev/System/Critical)
- Phase 3: Converters — WsBarWidth (MultiBinding), ArcGeometry (PathGeometry), GroupBarWidth, GroupPct, WsLevelBrush, WsIsHeavy, StringEquals
- Phase 4: RAM-Tab komplett neu — Hero (Donut+Spark+4 KPI-Cards), Primary/Secondary/Danger-Toolbar, 6 Filter-Chips (RadioButtons), DataGrid mit Microbars+Heavy-Badge+Status-Dot, Group-Header mit Bar+Pct-Badge, Footer mit Auswahl-Count + Trim/Kill
- Phase 5: Status-Pulse-Dot (Storyboard ScaleX/Y, Forever)
- Phase 6: Monitor-Auto-Höhe — ApplyTabSizing() beim Tab-Wechsel: SizeToContent.Height im Monitor, Restore ramHeight beim RAM. Äußerer ScrollViewer in MonitorView entfernt. RamWindowHeight persistiert separat.
- Phase 7: MemoryCleanupService.KillProcessAsync (Process.Kill + WaitForExit) mit Blocked-Guard

## Zieldateien (angepasst)
- Views/MainWindow.xaml — kompletter Rewrite RAM-Tab
- Views/MainWindow.xaml.cs — _ramHeight, ApplyTabSizing, ProcessGrid_SelectionChanged, TrimCurrentRow_Click
- Views/MonitorView.xaml — outer ScrollViewer entfernt (Grid VerticalAlignment=Top)
- ViewModels/MainViewModel.cs — Filter, Counts, Selection, Commands
- ViewModels/ProcessMemoryInfoViewModel.cs — Category/CategoryTag/IsSelected
- Services/ProcessClassifier.cs (neu) — Name-Lookup-Tabelle
- Services/MemoryCleanupService.cs — KillProcessAsync
- Services/AppSettings.cs — RamWindowHeight, ActiveFilter
- Converters/ — 7 neue Konverter (s.o.)

## Session 6 — Performance unter RAM-Druck
- **Hotpath**: `Process.MainModule.FileName` ist unter Speicherdruck extrem langsam (100–300 ms/Prozess). Bei 300 Prozessen × 5 s-Refresh → App permanent blockiert.
- **Fix**: `QueryFullProcessImageName` via bereits offenem Handle (`MemoryQueryService`), IsSystem- + Pfad-Cache pro PID. `ProcessIconService` nutzt denselben Pfad-Cache.
- **Pause-im-Tray**: `MainWindow.IsVisibleChanged` → `MainViewModel.SetWindowHidden()` stoppt den Refresh-Timer UND schaltet `Monitor.IsActive=false` (LHM-Sensoren laufen sonst unsichtbar weiter).
- Cache-Pruning: wenn >64 tote PIDs im Cache, werden sie beim nächsten Refresh rausgeputzt.
- Erwartet: Erster Refresh ~300 ms, weitere ~0 ms für gecachte PIDs.

## Session 5 — Splash-Animation
- SplashWindow (560×560, borderless, Topmost) zeigt 64×64 Design-Canvas via Viewbox
- Animation (4s): 3 Iso-Chips droppen gestaffelt (bottom → middle → top, easeInQuad), Pins zeichnen sich nach Impact, Accent-Linie am Top-Chip, Flash + Spark-Burst + Shake, Wordmark "ramdump-stack" fadet letter-by-letter ein
- CompositionTarget.Rendering + Stopwatch, 18 Spark-Ellipsen + 13 Letter-TextBlocks vorgeneriert
- Klick auf Splash = skip. Fade-Out zwischen 3.55 s und 3.95 s
- App.xaml: ShutdownMode=OnExplicitShutdown. App.OnStartup → Splash zuerst, auf Splash.Closed → MainWindow mit Settings-Position zeigen + Tray initialisieren

## Windows-Test Checkliste
1. `git pull && dotnet run` (Admin)
2. **Splash**: spielt 4 s, Chips droppen, Pins erscheinen, Flash + Funken, Wordmark fadet ein. Danach öffnet sich MainWindow an der gespeicherten Position. Klick auf Splash = skip.
3. RAM-Tab visuell: Donut-Arc bei Auslastung, Farbwechsel an Schwellen, KPI-Cards (Verfügbar/Cache/Commit/Standby), Hero-Sparkline (alle 5 s)
3. Chips filtern, Counts aktualisieren, Multi-Select (Ctrl/Shift) aktiviert Footer-Buttons
4. „Auswahl trimmen" → WS fallen; „Auswahl beenden" → Prozess weg (nach Bestätigung)
5. Gruppierung: Header mit Bar + Pct-Badge
6. Monitor-Tab: **keine leere untere Hälfte**, Fenster schrumpft auf Content-Höhe; Rückwechsel zu RAM: Höhe wiederhergestellt
7. Edge: ohne Admin → Trim/Kill disabled; Filter „crit" ohne Match → leere Liste + „0 von N"

## Bekannte Punkte / offen
- SizeToContent-Quirk: sollte sauber greifen; falls nicht → `InvalidateMeasure()` vor `SizeToContent.Height` ergänzen
- Pulse-Dot ScaleTransform geht bis 2.2 — kann minimal aus 10×10 Grid bluten, visuell ok
- ProcessClassifier-Namen-Liste (~35 Einträge): bei Drift neue Tags pflegen

## Design
- Icon: Resources/app-icon.svg + Resources/app.ico
- Farbsystem: COLORS.md — Ocker #D4A574 als Markenfarbe
- Load-Level-Schwellen: 60 / 80 / 92 % (Grün → Ocker → Orange → Rot)
