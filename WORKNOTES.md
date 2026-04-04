# ram-dump

## Stand
- Erstellt: 2026-04-04
- Status: v2 deployed, bereit zum Build-Test

## Struktur
- .NET 8 WPF, MVVM (CommunityToolkit.Mvvm)
- NuGet: CommunityToolkit.Mvvm 8.4.0, Hardcodet.NotifyIcon.Wpf 2.0.1
- ~30 Dateien

## v2 Features (neu)
- Exakter Cache via GetPerformanceInfo (statt 30%-Schätzung)
- Vorher/Nachher-Anzeige nach Cleanup ("24.1 GB -> 19.8 GB")
- Prozess-Gruppierung (Toggle, mit Summen-Header)
- Top-Wachstum: Prozesse mit >50MB Wachstum markiert (rot ▲)
- Prozess-Icons (aus exe extrahiert, gecached)
- Keyboard Shortcuts: F5, Ctrl+T, Ctrl+F, Ctrl+E
- CSV Export (SaveFileDialog)
- Settings persistieren (%APPDATA%/RamDump/settings.json)
- Bestätigungsdialog bei Voll-Bereinigung
- System Tray: Minimize to tray, 85%-Warnung
- Suchfeld mit Placeholder

## Offenes
- App-Icon (.ico) noch nicht erstellt
- CLAUDE.md Update (braucht /cadmin)
