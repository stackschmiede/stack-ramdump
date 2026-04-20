# ram-dump

## Stand
- 2026-04-20: Icon (Variant A) + ramdump-stack Farbsystem (COLORS.md) angewendet
- Status: bereit zum Build-Test unter Windows

## Struktur
- .NET 8 WPF, MVVM (CommunityToolkit.Mvvm)
- NuGet: CommunityToolkit.Mvvm 8.4.0, Hardcodet.NotifyIcon.Wpf 2.0.1

## v2 Features
- Exakter Cache via GetPerformanceInfo
- Vorher/Nachher-Anzeige nach Cleanup
- Prozess-Gruppierung (Toggle, Summen-Header)
- Top-Wachstum-Marker (▲ in Warning-Orange)
- Prozess-Icons (aus exe, gecached, Opacity 85%)
- Shortcuts: F5, Ctrl+T, Ctrl+F, Ctrl+E
- CSV Export, Settings-Persistenz, Tray-Icon

## Design (2026-04-20)
- Icon: `Resources/app-icon.svg` (Master) + `Resources/app.ico` (16/24/32/48/64/128/256)
  PNG-Set in `Resources/icons/`. Rebuild via `rsvg-convert` + Pillow.
- Farbsystem: `COLORS.md` — Ocker (#D4A574) als einzige Markenfarbe
- ProgressBar 4 Stufen (60/80/92): Grün → Ocker → Orange → Rot
- Voll-Bereinigung = destruktiv (Danger-Border), Toggles = Accent-Subtle

## Offenes
- Build-Test auf W11: `dotnet build` + Icon-Prüfung in Taskleiste (16/24 px)
- CLAUDE.md Update (braucht /cadmin)
