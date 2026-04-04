# RAM Dump

[![GitHub Release](https://img.shields.io/github/v/release/3motiBot/ram-dump)](https://github.com/3motiBot/ram-dump/releases/latest)

Windows 11 RAM-Manager zur Echtzeit-Ubersicht und sicheren Bereinigung des Arbeitsspeichers.

## Download

**[RamDump.exe herunterladen](https://github.com/3motiBot/ram-dump/releases/latest)** — Single-File, keine Installation noetig.

1. `RamDump.exe` herunterladen
2. Rechtsklick → **Als Administrator ausfuehren**
3. Fertig

> Ohne Admin-Rechte funktioniert die RAM-Analyse, Bereinigung ist dann deaktiviert.

## Features

- **Dashboard** — RAM-Nutzung, verfugbarer Speicher, Cache (Echtzeit via GetPerformanceInfo)
- **Prozessliste** — sortierbar, durchsuchbar, Auto-Refresh (3s)
- **Prozess-Gruppierung** — gleiche Prozesse zusammengefasst mit Gesamtsumme
- **Wachstum-Tracking** — Prozesse mit >50 MB Wachstum seit letztem Refresh markiert
- **Prozess-Icons** — automatisch aus der exe extrahiert
- **RAM-Bereinigung** — Working Set trimmen, Standby-Liste leeren, Voll-Bereinigung
- **Vorher/Nachher** — Cleanup zeigt "24.1 GB -> 19.8 GB (4.3 GB frei)"
- **Bestaetigungsdialog** — bei Voll-Bereinigung
- **CSV-Export** — Prozessliste exportieren
- **System Tray** — Minimize to tray, Warnung bei >85% RAM
- **Settings** — Fensterposition, Sortierung, Auto-Refresh persistent
- **Keyboard Shortcuts** — F5 Refresh, Ctrl+T Trim, Ctrl+F Suche, Ctrl+E Export
- **Dark Theme** — Catppuccin Mocha, durchgaengig inkl. Scrollbars und Kontextmenues

## Screenshot

![RAM Dump](https://github.com/user-attachments/assets/placeholder.png)

## Voraussetzungen

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Admin-Rechte fuer RAM-Bereinigung (Analyse funktioniert ohne)

## Installation

```powershell
# .NET 8 SDK installieren (falls nicht vorhanden)
winget install Microsoft.DotNet.SDK.8

# Repo klonen
git clone https://github.com/3motiBot/ram-dump.git
cd ram-dump

# Bauen und starten (als Administrator)
dotnet restore
dotnet build
Start-Process "bin\Debug\net8.0-windows\RamDump.exe" -Verb RunAs
```

## Technologie

| Komponente | Details |
|---|---|
| Framework | .NET 8, WPF |
| Pattern | MVVM (CommunityToolkit.Mvvm) |
| Tray | Hardcodet.NotifyIcon.Wpf |
| RAM-APIs | EmptyWorkingSet (psapi.dll), NtSetSystemInformation (ntdll.dll), GetPerformanceInfo (psapi.dll) |
| Theme | Catppuccin Mocha |

## Sicherheitskonzept

| Methode | Effekt | Risiko |
|---|---|---|
| Working Set trimmen | Seiten in Standby-Liste | Keins — Soft Page Fault holt zurueck |
| Standby-Liste leeren | Cache freigeben | Minimal — kurz langsamere Cache-Hits |

**Nicht implementiert** (bewusst): Modified Page List leeren (Datenverlust-Risiko), TerminateProcess (wuerde Programme beenden).

System-Prozesse (csrss, lsass, smss, svchost, dwm, etc.) sind auf einer Blockliste und werden nie angefasst.

## Projektstruktur

```
ram-dump/
├── App.xaml / App.xaml.cs          Startup, Tray, Dark Theme
├── RamDump.csproj                  .NET 8 WPF Projekt
├── app.manifest                    Admin-Rechte (UAC)
├── Models/
│   ├── ProcessMemoryInfo.cs        Prozess-Daten
│   ├── SystemMemoryInfo.cs         RAM-Status
│   └── CleanupResult.cs            Bereinigungsergebnis
├── Services/
│   ├── NativeMethods.cs            P/Invoke Deklarationen
│   ├── MemoryQueryService.cs       RAM-Daten lesen
│   ├── MemoryCleanupService.cs     RAM bereinigen
│   ├── ProcessIconService.cs       Icon-Extraktion
│   ├── SettingsService.cs          JSON Settings
│   └── AppSettings.cs              Settings POCO
├── ViewModels/
│   ├── MainViewModel.cs            MVVM ViewModel
│   ├── ProcessMemoryInfoViewModel.cs   Prozess-Wrapper
│   └── Messages/                   Messenger Messages
├── Views/
│   ├── MainWindow.xaml / .cs       Hauptfenster
│   └── ConfirmationDialog.xaml / .cs   Bestaetigungsdialog
├── Converters/                     WPF Value Converters
├── Resources/app.ico               App-Icon
└── deploy-w11.sh                   WSL -> Windows Deploy
```

## Lizenz

MIT
