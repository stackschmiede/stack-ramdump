# ram-dump

## Stand
- 2026-04-20 Session 3: Tab-Umbau vollständig abgeschlossen (alle 7 Phasen)
- Status: bereit zum Windows-Test — git pull → dotnet run (Admin)

## Struktur
- .NET 8 WPF, MVVM (CommunityToolkit.Mvvm)
- NuGet: CommunityToolkit.Mvvm 8.4.0, Hardcodet.NotifyIcon.Wpf 2.0.1
- Nächste Session: + LibreHardwareMonitorLib 0.9.4

## Was gebaut wurde (Session 2)
- Diff-Update statt Clear+Rebuild (Performance)
- Timer 5s, Icons nur für neue Prozesse
- Groups collapsible (ToggleButton ▶/▼)
- Groups starten collapsed
- Win-Prozesse ausblenden (ShowSystemProcesses, default false)
- IsSystemProcess via exe-Pfad C:\Windows\ + Namens-Fallback

## v2 Features (fertig)
- Exakter Cache via GetPerformanceInfo
- Vorher/Nachher-Anzeige nach Cleanup
- Prozess-Gruppierung (Toggle, Summen-Header, collapsible)
- Top-Wachstum-Marker (▲ in Warning-Orange)
- Prozess-Icons (gecached, Opacity 85%)
- Shortcuts: F5, Ctrl+T, Ctrl+F, Ctrl+E
- CSV Export, Settings-Persistenz, Tray-Icon

## Design
- Icon: Resources/app-icon.svg + Resources/app.ico
- Farbsystem: COLORS.md — Ocker (#D4A574) als Markenfarbe
- ProgressBar 4 Stufen (60/80/92): Grün → Ocker → Orange → Rot

## Session 3 — Was gebaut wurde
- Phase 1: LHM 0.9.4 NuGet, Authors/Company/Description
- Phase 2: Group-Icon (Items[0].Icon), PID→90, Work/Priv/Peak→140, Header rechtsbündig
- Phase 3: HardwareSensorService (LHM + PerformanceCounter-Fallback, SensorSnapshot record)
- Phase 4: MonitorViewModel (Timer-Lifecycle), CoreUsageViewModel, AppSettings erweitert
- Phase 5: TabControl-Root (RAM|Monitor|About), TabItem/TabControl Styles in App.xaml
- Phase 6: MonitorView (2×2 Grid, per-Core Balken, Pause/Intervall-Toolbar, LHM-Banner)
- Phase 7: AboutViewModel (WMI CPU/GPU/RAM/OS/Uptime), About-Tab XAML, Hyperlinks

## Nächste Schritte (nach Windows-Test)
- Windows-Test: git pull → dotnet run (Admin) — WinRing0-Treiber-Load beim 1. Monitor-Tab-Öffnen
- SmartScreen: "Trotzdem ausführen" → einmalig
- Issues für v2-Ideen anlegen (Sparklines, Power-Plan, Temp-Schwellen, Pagefile-Info)
