# Changelog

## v4.1.0

**Creator: Visual Polish**

- Warm fantasy palette with per-instrument accent colors (Piano, Harp, Lute, Bass)
- Note chips and piano keys now have rounded corners
- Playing and selected states shown as a subtle inset amber border instead of a bright glow
- Section markers redesigned with warm bronze color, rounded corners, and a gold left border
- Sharp notes rendered in a darker shade for easier identification at a glance
- Note sequencer now has a dark overlay header with clean separators
- Piano container uses a dark background with styled octave and REST buttons for better visibility
- Brighter ghost button text, octave labels, and section markers for improved contrast
- Instrument badge and per-instrument theming on the creator window

**Creator: Enhancements**

- Chord builder now caps at 7 notes per chord (covers up to a 13th chord) and silently ignores duplicate notes
- Chord preview shows a live count, e.g. `Chord (3/7): C:150 E:150 G:150`
- Clear button now requires a second click within 3 seconds to confirm, preventing accidental wipes of long transcriptions
- Notes window can no longer be closed independently of the Creator - the X button is disabled so the two windows always stay in sync

**Creator: Bug Fixes**

- REST button label is now vertically centered inside the button
- Dot button tooltip now refreshes when the selected note duration changes, instead of showing stale milliseconds

## v4.0.0

**Creator: Redesigned Layout**

- The notes panel now always opens in its own resizable window alongside the creator
- Compact creator window with streamlined controls
- Chord button uses a highlight toggle instead of ON/OFF text
- Click a selected note again to deselect it

**Creator: Section Markers**

- New "Section" button to add markers (Intro, Verse, Chorus, Bridge, Outro, or custom names)
- Section jump dropdown to quickly navigate between sections
- Sections are visually distinct full-width chips, separate from notes
- Sections persist with your song

**Creator: Playback Highlighting**

- Notes highlight as they play during Preview All and Preview Selected
- Auto-scrolls to follow playback
- Pause, resume, and stop controls in the notes window
- All editing controls are disabled during playback

**Creator: BPM Persistence**

- BPM now saves with your song and restores when you edit it

**Creator: Bug Fixes**

- Fixed chords not working with Replace mode
- Fixed scroll position resetting when adding notes
- Sharp notes now display in a darker shade for easier identification

## v3.6.0

**Creator: Insert & Replace Notes Anywhere**

- New "Insert" toggle button in the notes panel header — when active, piano keys insert notes after the selected note instead of appending to the end
- Right-click a single selected note and choose "Replace" to swap it with the next key you press
- Selection auto-advances after each insert, so you can type a sequence in the middle of a song

**Creator: Copy & Paste**

- Select notes, then right-click > "Copy" to copy them to an internal clipboard
- Right-click > "Paste" inserts the copied notes after the current selection (or at the end if nothing is selected)

**Creator: Dotted Notes**

- New "Dot" toggle next to the note duration buttons
- Adds 50% duration to any note type (e.g. dotted quarter = 1.5 beats)

**Creator: Full Undo**

- Undo now restores the previous state for any action (insert, delete, replace, paste, clear) — not just the last note added

**Creator: Expandable Notes Panel**

- New "Expand" button opens the notes panel in a separate, resizable window for more room while composing
- Piano keys in the creator still add notes to the expanded panel
- Close or click "Collapse" to return the panel to the creator window

**New Community Songs**

- Song of Healing - Koji Kondo
- A Thousand Miles - Vanessa Carlton
- Alicia - Tomas
- Lumiere - Tomas
- Leutinist of Limbo - Space Dandy

---

## v3.5.0

**Favorites**

- Star icon on each song card to mark favorites
- Click the star or right-click and select "Toggle Favorite" to add/remove
- Filter by "Favorites" in the source dropdown to show only starred songs
- Favorites persist across sessions

**New Community Songs**

- I'll Make A Man - Mulan (Lindsey)
- Elite Four - Pokemon Red/Blue/Yellow (Lindsey)
- Hey There Deliliah (RandomGuy)
- Sakura (Bai Gujing)

---

## v3.4.0

**Seek Slider**

- Added seek slider below the speed control in the now-playing panel
- Drag the slider to jump to any position in the song
- Elapsed and total time labels show song progress
- Clicking the slider pauses playback; releasing it seeks and resumes

**Octave Drift Fix**

- Increased octave change delays to reduce octave drift during long songs with many octave changes

**New Community Songs**
- Star Power SMB (Juliugh)
- Castle Vein (Arona)

---

## v3.3.0

**Note Selection in Maestro Creator**

Select individual notes or groups of notes in the Creator to preview or delete them without affecting the rest of your composition.

- Click a note chip to select it
- Shift+click to select a range of notes
- Ctrl+click to toggle individual notes in and out of the selection
- Right-click the notes area for quick actions: Preview Selected, Delete Selected, Select All, and Clear Selection
- Selected notes are highlighted with a gold border
- The header shows how many notes are currently selected
- "Preview All" button always plays the full song

---

## v3.2.0

**Scrolling Now Playing Title**

- Long song titles now scroll horizontally in the now-playing panel instead of being clipped
- Hover tooltip shows full title

**New Community Songs**

- The Best is Yet to Come MGS (Juliugh)
- Losing my religion (TwigLeigh)
- Wake me up when september ends (hilmo)
- Gravity Falls Theme slow (Georgius Agicolae)

---

## v3.1.0

**Improved Import Window**

- Redesigned import window with paste-from-clipboard button replacing the script textbox
- Import preview now shows note count and song duration before importing

**Bug Fixes**

- Fixed import of PianoTomas piano songs
- Fixed imported songs playing wrong notes by going into chord territory
- Fixed song duration display being inconsistent between community and main windows

---

## v3.0.2

**Bug Fixes**

- Fixed octave reset being too fast in Maestro Creator (10ms → 100ms delay between keypresses), causing GW2 to miss
  inputs when resetting to middle/low octave

---

## v3.0.1

**Bug Fixes**

- Fixed ClientId being visible in the module settings panel

---

## v3.0.0

**Community Song Sharing**

Share your songs with the community and download songs made by other players!

**How it works**

Submitted songs are added to a review branch. Songs are reviewed in batches and merged into main, making them available
in the community library for all players without requiring a module update.

**New Features**

- Upload your created or imported songs to the community library
- Browse, search, and download community songs
- Edit imported or created songs in the Maestro Creator (right-click → Edit Song)
- Re-upload edited songs to update your existing community submission
- Filter community songs by instrument and sort by name or date
- Filter your song list by source (Bundled, Created, Imported, Community)
- Maximum 3 uploads per day per user
- Duplicate detection prevents uploading identical songs
- Upload validation checks (name, transcriber, instrument, note count)
- Upload progress indicator with validation checklist

---

## v2.1.2

**Improvements**

- Added Bag of Holding support - corner icon position now persists between sessions

---

## v2.1.1

**Bug Fixes**

- Fixed AHK import timing to match original script playback
- Grace notes (instant key taps) now handled correctly instead of being bundled with sustained notes
- Reduced octave change delay during playback for faster response

---

## v2.1.0

**Playlist Queue**

Queue up songs and play them back-to-back!

**New Features**

- Queue drawer panel that slides out from main window
- Add songs to queue via right-click context menu
- Play, reorder, and remove songs from queue
- Continuous playback through queued songs
- Visual queue count badge on toggle button
- Instrument confirmation overlay when switching instruments between songs
- Current instrument display in the Now Playing panel
- Smart instrument matching - queue songs play directly when they match your current instrument

**Improvements**

- Consistent window styling across Community, Import, and Creator windows
- Shared theme constants for padding and spacing

---

## v2.0.1

**Bug Fixes**

- Auto-pause playing song when using Creator piano keyboard or octave controls
- Fixed note chip layout: consistent 3 chips per line with fixed width
- Scroll now shows last row of note chips fully

---

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

- New songs include: As Long As You Love Me, I Want It That Way, Total Eclipse Of The Heart, Still Loving You, River
  Flows In You, Numb, My Immortal, Perfect, and more
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
