<p align="center">
  <img width="256" height="256" alt="Aex Maestro" src="https://github.com/user-attachments/assets/d5fa3c1f-8359-4835-9c01-29faf5d45f17" />
</p>

<h1 align="center">Maestro</h1>

<p align="center">
  A <a href="https://blishhud.com">Blish HUD</a> module that plays music on Guild Wars 2 instruments.<br>
  Supports Piano, Harp, Lute, Bass, Flute, and Bell with lots of embedded songs.
</p>

## Features

- **Lots of embedded songs** ready to play across Piano, Harp, Lute, Bass, Flute, and Bell
- **Community song sharing** -- browse, download, and upload songs with other players
- **Maestro Creator** -- compose your own songs in-game with a visual piano keyboard editor
- **Playlist queue** -- line up songs and play them back-to-back
- **AHK import** -- bring in songs from AutoHotkey v1 scripts
- **Playback speed control** -- slow down for practice or speed up for fun (0.1x - 2.0x)
- **Smart playback** -- auto-pauses when you tab out or type in chat, resumes when you return
- Search, filter by instrument or source, and sort your library

## Maestro Creator

The Creator lets you compose songs in-game with a visual piano keyboard. Notes appear as colored chips in the sequence panel.

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
| `instrument` | yes | One of `Piano`, `Harp`, `Lute`, `Bass`, `Flute`, `Bell` (3-octave Choir Bell), `BellMagnanimous` (2-octave) |
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

## Module Settings

![Module settings - keybinds](https://github.com/user-attachments/assets/b38a401b-adb8-4a2e-b9ca-7a4a7ccbd26f)

![Module settings - options](https://github.com/user-attachments/assets/08843f71-3556-4b54-a00b-23da1996655b)

### Sharp Note Keybinds

Most song transcribers use **F1-F5 (Profession Skills)** for sharp notes. This avoids conflicts with NumPad 0-9, which GW2 uses for natural notes.

### GW2 Keybinds

Match your module settings to your in-game instrument keybinds. Sharp keybinds must not conflict with natural note keybinds -- the defaults avoid this by using NumPad for natural notes and Alt + number row for sharps.

![GW2 instrument keybind settings](https://github.com/user-attachments/assets/d35a5eb8-d301-4dc1-a405-4c50e4f5cc06)

## Get Involved

- **Feature or song requests?** Share them in [Ideas](https://github.com/uwponcel/Maestro/discussions/3)
- **Found a bug?** Open an [issue](https://github.com/uwponcel/Maestro/issues)

## Support

If you enjoy Maestro, consider supporting development:

- [Ko-fi](https://ko-fi.com/aex)
- In-game gold or items: **Aexor.6238**
