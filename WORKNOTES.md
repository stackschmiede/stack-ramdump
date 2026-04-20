# ram-dump

## Stand
- 2026-04-20 Session 2: Performance-Fix, Groups collapsible, Win-Prozesse-Filter, Plan für Tab-Umbau geschrieben
- Status: bereit für neue Session (Plan in ~/.claude/plans/ramdump-tabs.md)

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

## Nächste Session (Plan: ~/.claude/plans/ramdump-tabs.md)
- Tab-Umbau: RAM | Monitor | About
- Group-Header: App-Icon links vom Pfeil
- Spalten: Header rechtsbündig, PID 90px, Work/Priv/Peak 140px
- Monitor-Tab: CPU (%, per-Core, Temp), GPU (%, VRAM, Temp), Disk, Netz
- About-Tab: Nawinn Gutzeit, stackschmiede.de, Version
- LibreHardwareMonitorLib NuGet hinzufügen
