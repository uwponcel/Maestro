# Changelog

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
