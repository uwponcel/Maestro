# Maestro Creator Improvements - Design Spec

## Context

A community user provided 7 feedback items after using the Creator editor to transcribe a longer song. These range from bug fixes to new features. The improvements will be implemented in 5 phases, each independently testable.

## Summary of Items

| # | Item | Type | Phase |
|---|------|------|-------|
| 1 | Scrollbar resets when adding notes | Bug fix | 1 |
| 2 | BPM doesn't persist between sessions | Missing feature | 2 |
| 3 | No section markers for organizing notes | New feature | 3 |
| 4 | No note highlighting during preview playback | New feature | 4 |
| 5 | Octave switch timing issues | Skipped (game limitation) | - |
| 6 | Chords don't work with Replace mode | Bug fix | 1 |
| 7 | Phantom note on long notes + octave change | Investigation | 5 |

---

## Phase 1: Bug Fixes (Scroll Reset + Chord Replace)

### Item 6: Chord + Replace Fix

**Root cause:** `AddPendingChord()` in `MaestroCreatorWindow.cs` only checks insert mode, not replace mode. When chord mode and replace mode are both active, the chord always appends.

**Fix:** Add replace mode check to `AddPendingChord()`, before the insert mode check:

```csharp
if (_noteSequencePanel.IsReplaceMode)
{
    _noteSequencePanel.ReplaceAt(_noteSequencePanel.ReplaceTargetIndex, chordString);
    _noteSequencePanel.ExitReplaceMode();
}
else if (_noteSequencePanel.IsInsertMode && _noteSequencePanel.HasSelection)
{
    // existing insert logic
}
else
{
    _noteSequencePanel.AddNote(chordString);
}
```

**Files:** `UI/MaestroCreator/MaestroCreatorWindow.cs`

### Item 1: Scroll Position Fix

**Root cause:** `ReorderChipsFrom()` detaches/reattaches chips, causing `FlowPanel` to reset scroll. No scroll-to logic exists after adding/inserting notes.

**Fix:**

1. Save/restore scroll offset in `ReorderChipsFrom()`:
   ```csharp
   var savedScroll = _chipsContainer.VerticalScrollOffset;
   // ... detach/reattach logic ...
   _chipsContainer.VerticalScrollOffset = savedScroll;
   ```

2. Add deferred scroll-to-chip mechanism:
   - `_pendingScrollToIndex` field
   - `ScrollToChip(int)` method that calculates proper offset
   - `UpdateContainer()` override to process deferred scroll
   - Call from `AddNote()`, `InsertAt()`, `InsertRange()`, `ReplaceAt()`

**Files:** `UI/MaestroCreator/NoteSequencePanel.cs`

**How to test:**
- Add 50+ notes, continue adding - panel stays at latest note
- Insert mode: insert in middle - panel scrolls to insertion point
- Replace a note - panel stays in place

---

## Phase 2: BPM Persistence

**Root cause:** `Song` model has no BPM field. BPM is local to `DurationSelector`, defaults to 120 every time.

**Fix:**

1. Add `int? Bpm` to `Song.cs` (nullable for legacy songs)
2. Add `"bpm"` to `SongCompactJsonDto` in `SongSerializer.cs` with `NullValueHandling.Ignore`
3. Set `song.Bpm = _durationSelector.Bpm` in `BuildSong()`
4. Restore `_durationSelector.Bpm = song.Bpm.Value` in `LoadSong()`

LiteDB auto-persists the new property since it stores the full Song as BSON.

**Files:** `Models/Song.cs`, `Services/Data/SongSerializer.cs`, `UI/MaestroCreator/MaestroCreatorWindow.cs`

**How to test:**
- Create song with BPM 90, save, restart game, edit - BPM shows 90
- Import JSON without `"bpm"` field - defaults to 120

---

## Phase 3: Section Markers

**Design:** Section markers are special note strings `[SectionName]` stored in `Song.Notes`. `NoteParser` already skips them (no regex match, `notes.Count == 0` triggers `continue`).

### BaseChip refactor

Create `BaseChip` abstract class with shared properties:
- `Index`, `IsSelected`, `IsHighlighted`, `ChipClicked`, `RemoveClicked`

`NoteChip` and new `SectionMarkerChip` both inherit from `BaseChip`. The `_chips` list in `NoteSequencePanel` changes from `List<NoteChip>` to `List<BaseChip>`.

### SectionMarkerChip

Full-width chip with distinct color (section blue/teal), displays section name, close button. Editable via double-click or context menu.

### NoteSequencePanel changes

- Add "Section" button to header
- Section name input via `ContextMenuStrip` with common names (Intro, Verse, Chorus, Bridge, Outro) + Custom
- Add section jump `Dropdown` (visible when sections exist)
- `UpdateSectionDropdown()` scans notes for `[markers]`, populates dropdown
- Dropdown selection triggers `ScrollToChip()` from Phase 1

### Detection helpers

```csharp
public static bool IsSectionMarker(string s) => s?.StartsWith("[") == true && s.EndsWith("]");
public static string GetSectionName(string s) => s.Substring(1, s.Length - 2);
```

**Files:**
- NEW: `UI/MaestroCreator/BaseChip.cs`
- NEW: `UI/MaestroCreator/SectionMarkerChip.cs`
- MODIFY: `UI/MaestroCreator/NoteChip.cs` (inherit BaseChip)
- MODIFY: `UI/MaestroCreator/NoteSequencePanel.cs`

**How to test:**
- Add section "Verse 1", add notes, add section "Chorus", add notes
- Jump dropdown navigates to sections
- Save/reopen - sections persist
- Preview plays correctly (sections skipped)

---

## Phase 4: Playback Highlighting

**Design:** During Creator preview, highlight the currently-playing note chip with a distinct color (green border) and auto-scroll.

### Command-to-note mapping

Add `ParseWithMapping()` to `NoteParser` that returns both commands and a `int[] CommandToNoteLineIndex` mapping each command back to its source note line. Section markers are skipped in the mapping.

### Wiring

1. Expose `SongPlayer` as internal property on `Module`
2. In `MaestroCreatorWindow`, when preview starts:
   - Parse with mapping, store mapping
   - Set `_isPreviewActive = true`
3. In `UpdateContainer()`, poll `SongPlayer.CurrentCommandIndex`, map to note index, call `_noteSequencePanel.HighlightPlayingNote(index)`
4. On preview stop/complete, call `ClearPlaybackHighlight()`

### Visual

`BaseChip.IsHighlighted` draws a green border. Highlight takes priority over selection amber border.

### Selection preview mapping

For "Preview Selected", the mapping indices are relative to the subset. Store original note indices and map back.

**Files:**
- `Services/Data/NoteParser.cs` (add `ParseResult`, `ParseWithMapping()`)
- `UI/MaestroCreator/MaestroCreatorWindow.cs` (wire highlighting)
- `UI/MaestroCreator/NoteSequencePanel.cs` (add `HighlightPlayingNote`, `ClearPlaybackHighlight`)
- `UI/MaestroCreator/BaseChip.cs` (add `IsHighlighted`)
- `Module.cs` (expose `SongPlayer`)

**How to test:**
- Preview All with 20+ notes - green highlight follows playback, auto-scrolls
- Preview Selected - only selected notes highlight
- Stop mid-song - highlight clears
- Chords highlight for full duration

---

## Phase 5: Phantom Note Investigation

**Approach:** Debug logging only, no behavior changes.

Add detection in `SongPlayer.PlaybackLoop()` for the pattern: long Wait (>500ms) followed by KeyUp followed by octave KeyDown. Log timing details to the debug log.

Optionally add `PostLongNoteGapMs = 0` constant in `GameTimings.cs` as a future tuning knob.

**Files:**
- `Services/Playback/SongPlayer.cs` (pattern detection, logging)
- `Models/GameTimings.cs` (optional gap constant)

**How to test:**
- Create song at 70 BPM with half notes crossing octaves
- Play in debug mode, check log for `[PHANTOM-RISK]` entries

---

## Dependency Graph

```
Phase 1 (scroll + chord fix) -- no dependencies
Phase 2 (BPM persistence)    -- no dependencies
Phase 3 (section markers)    -- depends on Phase 1 (ScrollToChip)
Phase 4 (playback highlight) -- depends on Phase 3 (BaseChip)
Phase 5 (phantom logging)    -- independent
```

Execution order: 1 -> 2 -> 3 -> 4 -> 5 (user tests after each phase)
