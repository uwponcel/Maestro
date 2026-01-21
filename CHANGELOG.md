# Changelog

## v2.0.0

**Maestro Creator**

Create your own songs directly in-game with a visual piano keyboard editor!

**New Features**

- Interactive piano keyboard with clickable keys for composing
- Support for all instruments: Piano, Harp, Lute, and Bass
- Duration selector with musical note values (whole, half, quarter, eighth, sixteenth)
- Chord mode for adding multiple notes simultaneously
- Live preview of your composition
- Note sequence panel with visual chips showing notes, octaves, and durations
- Undo and clear functionality for easy editing
- Automatic octave reset when opening the creator

**Improvements**

- Reorganized UI into Main, Community, and MaestroCreator folders
- Consolidated song storage into unified SongStorage class
- Multi-instrument octave support (Bass uses 2 octaves, others use 3)

**Internal**

- Centralized note-to-key mappings in NoteMapping
- Added GameTimings for consistent timing constants
- Refactored KeyboardService for direct note playback

---

## v1.5.2

**Maintenance Release**

- Disabled Community button (Coming soon!)
- Reduced module package size by ~7MB (removed bundled core dependencies)
- Added LiteDB for future song querying capabilities

---

## v1.5.1

**101! Songs!**

The song library has grown to 101 embedded songs! 😀

**Improvements**

- Auto-pause playback when typing in Maestro's search box or other overlay text inputs
- Auto-resume playback when clicking outside the search box
- Improved README documentation with clear keybind tables for natural and sharp notes
- Better settings description explaining sharp note keybind restrictions

**Fixes**

- Fixed transcriber name attribution (PianoThomas -> PianoTomas)

**Internal**

- Cleaned up project file organization
- Standardized script naming to lowercase kebab-case

---

## v1.5.0

**Piano Support & New Songs**

Added full piano support with 20 new piano songs converted from PianoTomas transcriptions!

- New songs include: As Long As You Love Me, I Want It That Way, Total Eclipse Of The Heart, Still Loving You, River Flows In You, Numb, My Immortal, Perfect, and more
- Sharp note support for piano using Alt+NumPad or Profession Skills (F1-F5)
- Updated keybind tooltips to clarify GW2 binding requirements

**Song Organization**

- Songs now organized into instrument subfolders (Bass, Harp, Lute, Piano)
- Easier browsing and management of the song library

**Bug Fixes**

- Fixed stuck notes when stopping or pausing playback mid-song
- Fixed timing issue in AHK conversion that caused songs to play too fast

---

## v1.4.0

**Smart Playback Controls**

- Auto-pause when GW2 loses focus (tabbing out)
- Auto-pause when typing in chat - resumes when clicking back in game
- Playback speed slider (0.1x to 2.0x) for practice or fun

**Improved Filtering**

- Combined filter dropdown with two sections (Source + Instrument)
- Filter by Bundled/Imported songs alongside instrument type

**New Songs**

Added 11 new songs including Interstellar, Never Gonna Give You Up, Radioactive, Song of Storms, and more!

---

## v1.3.1

🎵 **Expanded Song Library**

Added 17 new songs bringing the total to 87 embedded songs!

- Removed BPM-based timing in favor of direct millisecond values for more accurate playback
- Songs now display transcriber attribution (Artist - Transcriber format)
- Streamlined AHK import process with improved timing conversion

---

## v1.3.0

📥 **Song Import**

You can now import your own songs from AHK v1 scripts!

- Import songs from AutoHotkey v1 macro scripts
- User-imported songs are saved locally and persist across sessions
- Right-click on imported songs to delete them
- Imported songs show in the song list alongside embedded songs

🎨 **UI Improvements**

- Redesigned Import window with cleaner layout
- Consistent button sizing across all windows
- Improved spacing and visual consistency

---

## v1.2.1

🐛 **Bug Fixes**

- Fixed songs not loading for users (ContentsManager approach for .bhm packages)

---

## v1.2.0

🎵 **Compact Song Format**

All 70 songs have been migrated to a new compact notation format, making them easier to read and edit.

- 80% smaller file sizes
- Songs now use musical notation (C:4 D#:8 R:2) instead of verbose command lists
- BPM-based timing - Tempo is now defined per song, making timing more intuitive
- Human-readable - Edit songs with standard note names and durations

*This rework lays the groundwork for a future song creation feature!*

🐛 **Bug Fixes**

- Fixed songs not loading on module startup

---

## v1.1.0

⌨️ **Customizable Key Remapping**

You can now remap all instrument keys in module settings to match your GW2 keybinds.

- Natural Notes - Remap C, D, E, F, G, A, B, C+, Octave Up/Down
- Sharp Notes - Configure key + modifier combinations (Alt, Ctrl, Shift)

🐛 **Bug Fixes**

- Fixed sharp notes not releasing properly

---

## v1.0.0

**Initial Release**

Maestro lets you play music on GW2 instruments with 70 embedded songs. Supports Piano, Harp, Lute, and Bass.

- 70 embedded songs ready to play
- Supports Piano, Harp, Lute, and Bass instruments
- Search songs by name
- Filter by instrument
- Play, pause, and stop controls
