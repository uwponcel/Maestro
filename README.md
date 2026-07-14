<p align="center">
  <img width="256" height="256" alt="Aex Maestro" src="https://maestro-assets.pages.dev/maestro-logo.png" />
</p>

<h1 align="center">Maestro</h1>

<p align="center">
  A <a href="https://blishhud.com">Blish HUD</a> module that plays music on Guild Wars 2 instruments.<br>
  Supports Piano, Harp, Lute, Bass, Flute, Bell, and Drum Set with lots of embedded songs.
</p>

## Features

- **Lots of embedded songs** ready to play across Piano, Harp, Lute, Bass, Flute, Bell, and Drum Set
- **Practice Mode** -- a Guitar Hero-style note highway that grades your timing as you play the real instrument
- **Community song sharing** -- browse, download, and upload songs with other players
- **Maestro Creator** -- compose your own songs in-game with a visual piano keyboard editor
- **Playlist queue** -- line up songs and play them back-to-back
- **AHK import** -- bring in songs from AutoHotkey v1 scripts
- **Playback speed control** -- slow down for practice or speed up for fun (0.1x - 2.0x)
- **Smart playback** -- auto-pauses when you tab out or type in chat, resumes when you return
- Search, filter by instrument or source, and sort your library

## Practice Mode

Practice any melodic song in the modern note format on a falling-note highway. Tiles scroll down toward a hit line and you press your configured instrument keys in time, playing the real in-game instrument.

### How to use it

1. Click the bullseye button on a song card (it lights up while that song's practice window is open).
2. Equip the song's instrument in GW2, then click **Ready**.
3. Play along as the tiles fall -- press the matching keybind when a tile's head crosses the hit line.
4. When the song ends, a results screen shows your final score, max combo, and a Perfect/Good/Miss/Wrong breakdown, with buttons to Restart or Close.

![Practice Mode](https://maestro-assets.pages.dev/practice-mode.gif)

### Features

- **Your keybinds** -- grades the keys you already configured for Maestro; the bottom strip shows your key per lane, and tile colors match your GW2 note-skill icons
- **Readable tiles** -- bright note head at the hit moment, dimmer sustain tail, note letters on every tile
- **Sharps like in-game** -- darker `#` tiles, played with **Alt + skill slots 1-5** (C#/D#/F#/G#/A#), same as GW2
- **Grades** -- Perfect (50 ms), Good (120 ms), Miss, with live score and combo, plus a results screen when the song ends
- **Speed control** (0.5x / 0.75x / 1.0x) and a 3-2-1 countdown on start and restart
- **Section loop** -- Shift+click the loop end, Ctrl+click the loop start, right-click to clear
- **Automatic octave switching** so you can focus on the melody (manual handling planned)

Drum Set songs can't be practiced (the highway is melodic-only).

## Maestro Creator

The Creator lets you compose songs in-game with a visual piano keyboard. Notes appear as colored chips in the sequence panel.

![Maestro Creator](https://maestro-assets.pages.dev/maestro-creator.gif)

### Note Selection

You can select notes to preview or delete specific sections of your composition:

- **Click** a note chip to select it
- **Shift+Click** to select a range of notes
- **Ctrl+Click** to add or remove individual notes from the selection
- **Right-click** the notes area for a context menu with Preview Selected, Delete Selected, Select All, and Clear Selection

## Importing Songs

Open the Import window from the main window and click **Paste Song**. Maestro auto-detects the format of whatever is on your clipboard:

- **AHK v1 script** -- the format exported by the in-game Music Box and AutoHotkey.
- **Maestro song (JSON)** -- Maestro's own format, documented below.

The fields fill in automatically. Adjust the title, artist, transcriber, or instrument if you like, then click Import.

### Maestro Song Format

A Maestro song is a single JSON object:

```json
{
  "name": "Song name",
  "artist": "Artist",
  "transcriber": "Your name",
  "instrument": "Piano",
  "notes": ["G-:333", "A-:333", "A#-:166", "R:666"],
  "skipOctaveReset": false
}
```

| Field | Required | Description |
|---|---|---|
| `name` | yes | Song title |
| `artist` | no | Composer or artist (defaults to "Unknown") |
| `transcriber` | no | Who arranged it for GW2 |
| `instrument` | yes | One of `Piano`, `Harp`, `Lute`, `Bass`, `Flute`, `Bell` (3-octave Choir Bell), `BellMagnanimous` (2-octave), `DrumSet` |
| `notes` | yes | The note sequence (see below) |
| `skipOctaveReset` | no | Skip the octave reset at the start of playback (default `false`) |

Each entry in `notes` is `Note[#][+/-][^]:DurationMs`:

- **Note** -- `C D E F G A B`, or `R` for a rest (silence)
- `#` -- sharp (e.g. `C#`)
- `+` / `-` -- high / low octave (omit for the middle octave)
- `^` -- high C (the 8th key)
- `:DurationMs` -- how long to hold the note, in milliseconds

Play notes together as a chord by separating them with spaces in one entry: `"C:470 E:470 G:470"`.

Examples: `C:150` (middle C for 150ms), `F#:300` (F sharp), `G+:200` (high-octave G), `R:500` (half-second rest).

### Drum Set Format

The Drum Set is percussion: one fixed octave, no sharps. Each note line is one
beat slot. Hit sounds together by space-joining full tokens on a line, each with
its own duration (same as melodic chords) - e.g. `b:250 hc:250` is kick + hi-hat.
Duration in milliseconds. Rest is `R`.

| Code | Sound | Code | Sound |
|------|-------|------|-------|
| `b`  | Bass drum   | `cr` | Crash cymbal |
| `s`  | Snare       | `rd` | Ride cymbal |
| `x`  | Cross-stick | `hc` | Hi-hat closed |
| `g`  | Ghost snare | `ho` | Hi-hat open |
| `ht` | High tom    | `hf` | Hi-hat foot |
| `mt` | Mid tom     | `R`  | Rest |
| `ft` | Floor tom   |      |  |

Example (one bar of a basic rock beat at 120 BPM):

    b:250 hc:250
    hc:250
    s:250 hc:250
    hc:250

## Module Settings

![Module settings - keybinds](https://maestro-assets.pages.dev/module-settings-keybinds.png)

![Module settings - options](https://maestro-assets.pages.dev/module-settings-options.png)

### Sharp Note Keybinds

Most song transcribers use **F1-F5 (Profession Skills)** for sharp notes. This avoids conflicts with NumPad 0-9, which GW2 uses for natural notes.

### GW2 Keybinds

Match your module settings to your in-game instrument keybinds. Sharp keybinds must not conflict with natural note keybinds -- the defaults avoid this by using NumPad for natural notes and Alt + number row for sharps.

![GW2 instrument keybind settings](https://maestro-assets.pages.dev/gw2-instrument-keybinds.png)

## Get Involved

- **Feature or song requests?** Share them in [Ideas](https://github.com/uwponcel/Maestro/discussions/3)
- **Found a bug?** Open an [issue](https://github.com/uwponcel/Maestro/issues)

## Support

If you enjoy Maestro, consider supporting development:

- [Ko-fi](https://ko-fi.com/aex)
- In-game gold or items: **Aexor.6238**
