# ramdump-stack · Farbsystem

Farbpalette abgestimmt auf das Stackschmiede-Logo.
Drei dunkle Grau-Stufen + Creme für Text, **Ocker (#D4A574) als einzige Akzentfarbe**.
Semantische Farben nur dort, wo sie wirklich Information tragen (Auslastungs-Warnstufen, Export/Erfolg, Fehler).

---

## 1. Basis — Hintergründe & Chrome

Aus dem Logo übernommen. Drei Stufen reichen für die gesamte App: Fenster, Card, Hover/Selection.

| Token | Hex | Verwendung |
|---|---|---|
| `--bg-base` | `#0F0F10` | Äußerer Fensterhintergrund, Titlebar |
| `--bg-surface` | `#141415` | Toolbar, Tabellen-Body, KPI-Card-Fläche |
| `--bg-raised` | `#1A1A1C` | KPI-Cards (gehoben), Gruppen-Header, Hover-Rows |
| `--bg-hover` | `#22221F` | Row-Hover (ganz leicht warmer Stich durch Ocker-Beimischung) |
| `--bg-selected` | `#2A2520` | Ausgewählte Row / aktives Filter-Chip |

**Grund:** Die drei Logo-Grautöne (`#0F0F10`, `#141415`, `#1A1A1C`) liefern bereits eine fertige Tiefen-Hierarchie. Für Interaktionszustände (Hover, Selection) wird minimal Ocker beigemischt, damit die App einen Identitäts-Grundton bekommt, ohne blau zu werden wie jede andere Windows-Anwendung.

---

## 2. Linien & Trennung

| Token | Hex | Verwendung |
|---|---|---|
| `--border-subtle` | `#26262A` | Card-Borders, Tabellen-Zeilen-Trenner |
| `--border-default` | `#33332E` | Input-Border, Button-Border (Default) |
| `--border-strong` | `#4A4740` | Input-Border focus, Toolbar-Trennlinie |
| `--divider` | `#1E1E20` | Horizontale Tabellen-Trenner (sehr dezent) |

Alle Borders sind warm getönt (Richtung `#EDEAE3`), nicht kalt-neutral — das ist der subtile Unterschied zu Standard-Windows-Dark.

---

## 3. Text

| Token | Hex | Kontrast auf `--bg-surface` | Verwendung |
|---|---|---|---|
| `--text-primary` | `#EDEAE3` | 14.2 : 1 | Werte, Tabellen-Hauptspalten, Überschriften |
| `--text-secondary` | `#B8B3A8` | 8.0 : 1 | Labels (`RAM NUTZUNG`, `CACHE`), sekundäre Zahlen |
| `--text-tertiary` | `#7A766E` | 4.1 : 1 | Statuszeile „Bereit", Platzhalter, Unit-Suffix „GB" |
| `--text-disabled` | `#4E4B45` | — | Inaktive Buttons, disabled Controls |
| `--text-on-accent` | `#1A1407` | 12.1 : 1 auf Ocker | Text auf ockerfarbenen Flächen |

Creme statt reinweiß — spart Augenbelastung bei langem Monitoring und passt zum Logo.

---

## 4. Akzent — Ocker

**Einzige Markenfarbe.** Sparsam verwenden: primäre Action-Buttons, aktiver Toggle-State, Fokus-Ring, Hyperlinks. **NICHT** für Auslastungs-Progressbar (das ist eine semantische Information, kein Branding).

| Token | Hex | Verwendung |
|---|---|---|
| `--accent` | `#D4A574` | Primary-Button-Text/Icon, aktiver Toggle-State, Fokus-Ring |
| `--accent-hover` | `#E3B789` | Hover auf Akzent-Elementen |
| `--accent-pressed` | `#B8895A` | Pressed-State |
| `--accent-subtle` | `#D4A57422` | Hintergrund aktiver Toggles (Gruppieren, Auto-Refresh), Badges |
| `--accent-ring` | `#D4A57466` | Fokus-Ring um Inputs (2 px) |

**Regel:** Pro Screen max. 1 dominanter Ocker-Touchpoint + Fokus-Ring. Wenn alles leuchtet, leuchtet nichts.

---

## 5. Semantik — RAM-Auslastung

Der Progressbar-Zustand ist die einzige Stelle, an der Farbe *funktional* wird. Grün → Amber → Rot skaliert mit der Auslastung, damit der Zustand peripher erkennbar ist.

| Zustand | Schwelle | Token | Hex | Einsatz |
|---|---|---|---|---|
| Gesund | < 60 % | `--load-ok` | `#6FA26A` | Progressbar-Fill, Icon-Glyphe „Verfügbar" |
| Erhöht | 60 – 80 % | `--load-warn` | `#D4A574` | Progressbar-Fill (= Akzent, bewusst) |
| Kritisch | 80 – 92 % | `--load-high` | `#D98A5C` | Progressbar-Fill, Warn-Badge |
| Voll | > 92 % | `--load-critical` | `#C25A4E` | Progressbar-Fill, Zahl `26,1` rot tönen |

**Track:** `--load-track: #26262A` (identisch `--border-subtle`).

Das aktuelle Pink (~84 %) wird durch `--load-high` (warmes Rot-Orange) ersetzt — bleibt laut genug, um „Achtung" zu signalisieren, ohne aus der Palette zu fallen.

---

## 6. Semantik — allgemein

Für Toasts, Dialoge, Inline-Messages. Alle Werte sind gedimmt, damit sie auf dunklem Grund nicht schreien.

| Token | Hex | Bg (für Pills) | Verwendung |
|---|---|---|---|
| `--success` | `#6FA26A` | `#6FA26A1F` | „Export erfolgreich", Cache-Zahl |
| `--info` | `#7D9DB5` | `#7D9DB51F` | Info-Toast, Tooltip-Akzent |
| `--warning` | `#D98A5C` | `#D98A5C1F` | „Trimmen beansprucht RAM" |
| `--danger` | `#C25A4E` | `#C25A4E1F` | Fehler, Voll-Bereinigung-Warnung |

Alle vier Werte sind **im gleichen Sättigungs- und Helligkeitsbereich** (OKLCH-L ≈ 65 %, C ≈ 0.09), damit sie nebeneinander wie ein Set wirken und nicht wie ein Ampel-Sticker.

---

## 7. Kontrollen — konkrete Mappings

### Buttons

| Typ | Bg | Border | Text |
|---|---|---|---|
| Default (Standby Leeren, Aktualisieren…) | `transparent` | `--border-default` `#33332E` | `--text-primary` `#EDEAE3` |
| Default hover | `--bg-raised` `#1A1A1C` | `--border-strong` `#4A4740` | `--text-primary` |
| Primary (selten, nur wenn eine Aktion wirklich heraussticht) | `--accent` `#D4A574` | keine | `--text-on-accent` `#1A1407` |
| Toggle aktiv (Gruppieren, Auto-Refresh) | `--accent-subtle` `#D4A57422` | `--accent` `#D4A574` | `--accent` `#D4A574` |
| Destructive (Voll-Bereinigung) | `transparent` | `--danger` `#C25A4E` | `--danger` |

### Input (Suche)

| State | Bg | Border | Text |
|---|---|---|---|
| Default | `--bg-base` `#0F0F10` | `--border-default` `#33332E` | `--text-secondary` |
| Focus | `--bg-base` | `--accent` `#D4A574` + Ring `--accent-ring` | `--text-primary` |

### Tabelle

| Element | Farbe |
|---|---|
| Header-Bg | `--bg-surface` `#141415` |
| Header-Text | `--text-secondary` `#B8B3A8`, uppercase optional |
| Row-Bg (odd/even) | beide `--bg-base` `#0F0F10` — **kein Zebra** (ruhiger) |
| Row-Hover | `--bg-hover` `#22221F` |
| Row-Selected | `--bg-selected` `#2A2520`, linke 2 px Border in `--accent` |
| Gruppen-Header | `--bg-raised` `#1A1A1C`, Text `--text-primary`, Count in `--text-tertiary` |
| Zahlenspalten | `--text-primary`, tabular-nums |
| Einheit (GB/MB) | `--text-tertiary` |

### KPI-Card

| Element | Farbe |
|---|---|
| Card-Bg | `--bg-raised` `#1A1A1C` |
| Card-Border | `--border-subtle` `#26262A` |
| Label („RAM NUTZUNG") | `--text-secondary`, 11 px, `letter-spacing: 0.08em`, uppercase |
| Großer Wert | `--text-primary` `#EDEAE3`, 28 px, `font-weight: 600` |
| Referenzwert („/ 31,1 GB") | `--text-tertiary` `#7A766E` |
| Subzeile („5,0 GB") | `--text-secondary` |

---

## 8. CSS-Variablen — copy-paste

```css
:root {
  /* Base */
  --bg-base:       #0F0F10;
  --bg-surface:    #141415;
  --bg-raised:     #1A1A1C;
  --bg-hover:      #22221F;
  --bg-selected:   #2A2520;

  /* Borders */
  --border-subtle:  #26262A;
  --border-default: #33332E;
  --border-strong:  #4A4740;
  --divider:        #1E1E20;

  /* Text */
  --text-primary:    #EDEAE3;
  --text-secondary:  #B8B3A8;
  --text-tertiary:   #7A766E;
  --text-disabled:   #4E4B45;
  --text-on-accent:  #1A1407;

  /* Accent */
  --accent:          #D4A574;
  --accent-hover:    #E3B789;
  --accent-pressed:  #B8895A;
  --accent-subtle:   #D4A57422;  /* 13% alpha */
  --accent-ring:     #D4A57466;  /* 40% alpha */

  /* Load states */
  --load-ok:        #6FA26A;
  --load-warn:      #D4A574;
  --load-high:      #D98A5C;
  --load-critical:  #C25A4E;
  --load-track:     #26262A;

  /* Semantic */
  --success:  #6FA26A;
  --info:     #7D9DB5;
  --warning:  #D98A5C;
  --danger:   #C25A4E;
}
```

---

## 9. Was konkret in deinem aktuellen Screen anders wird

1. **Pink Progressbar → `--load-high` `#D98A5C`** (warmes Rot-Orange bei 84 %). Fällt nicht mehr aus der Palette.
2. **Zwei verschiedene Grüntöne (5,0 GB / 2,8 GB) → `--load-ok` `#6FA26A`** einheitlich. Oder: Card-Werte komplett in `--text-primary` und den Grün-Akzent nur auf „5,0 GB" wenn positiv.
3. **Button-Toggles „Gruppieren" / „Auto-Refresh" → Ocker-Toggle-Style** (gefüllt mit `--accent-subtle`, Border & Text in `--accent`). Gibt der App ihre Markenfarbe.
4. **Toolbar-Buttons (Alles Trimmen, Standby Leeren …) → Default-Button-Style** (transparent + Border). Nicht mehr hervorgehoben als die Toggle-Chips.
5. **Statuszeile „Bereit" → `--text-tertiary`** statt Creme-weiß.
6. **Firefox-Icons und andere Prozess-Icons**: Originalfarben lassen, aber auf 85 % Opacity — sonst konkurrieren sie mit dem Akzent.

---

## 10. Dos & Don'ts

- ✅ Ocker ist die **einzige** Markenfarbe. Nicht zusätzlich Blau/Lila einführen.
- ✅ Statuszeile, Units, Sekundär-Labels immer in Tertiär-Text — verhindert „alles gleich laut".
- ✅ Progressbar-Farbe **an Schwellwert koppeln**, nicht zufällig wählen.
- ❌ Keine Gradients auf Buttons oder Cards — das Logo ist flach, die App auch.
- ❌ Kein reines Weiß (`#FFF`) — immer Creme `#EDEAE3`.
- ❌ Keine Schatten mit schwarzem Shadow auf schwarzem Grund; wenn Tiefe nötig, über die Bg-Stufen lösen.
