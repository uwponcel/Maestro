using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Maestro.Services.Playback;
using Maestro.UI.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Maestro.UI.MaestroCreator
{
    public class NoteSequencePanel : Panel
    {
        public static class Layout
        {
            public const int HeaderHeight = 26;
            public const int HeaderBottomSpacing = 12;
            public const int ChipSpacing = 4;
            public const int Padding = 5;
            public const int PaddingBottom = 30;
            public const int ButtonY = 6;
        }

        public event EventHandler SequenceChanged;
        public event EventHandler UndoClicked;
        public event EventHandler ClearClicked;
        public event EventHandler SelectionChanged;
        public event EventHandler PreviewAllRequested;
        public event EventHandler PreviewSelectionRequested;
        public event EventHandler PauseRequested;
        public event EventHandler StopRequested;
        public event EventHandler InsertModeChanged;
        public event EventHandler ReplaceModeChanged;

        private readonly List<string> _notes = new List<string>();
        private readonly List<BaseChip> _chips = new List<BaseChip>();

        private readonly HashSet<int> _selectedIndices = new HashSet<int>();
        private int _lastClickedIndex = -1;

        private bool _isInsertMode;
        private bool _isReplaceMode;
        private int _replaceTargetIndex = -1;
        private readonly List<string> _clipboard = new List<string>();
        private readonly Stack<List<string>> _undoStack = new Stack<List<string>>();
        private int _pendingScrollToIndex = -1;
        private int _scrollApplyFrames;
        private Scrollbar _scrollbarRef;

        private static readonly FieldInfo PanelScrollbarField =
            typeof(Panel).GetField("_panelScrollbar", BindingFlags.NonPublic | BindingFlags.Instance);

        private Scrollbar GetScrollbar()
        {
            if (_scrollbarRef != null && _scrollbarRef.Parent != null) return _scrollbarRef;
            _scrollbarRef = PanelScrollbarField?.GetValue(_chipsContainer) as Scrollbar;
            return _scrollbarRef;
        }

        private void RequestScrollTo(int chipIndex)
        {
            _pendingScrollToIndex = chipIndex;
            _scrollApplyFrames = 5;
        }

        private readonly ContextMenuStrip _contextMenu;
        private readonly ContextMenuStripItem _previewSelectedItem;
        private readonly ContextMenuStripItem _deleteSelectedItem;
        private readonly ContextMenuStripItem _replaceItem;
        private readonly ContextMenuStripItem _copyItem;
        private readonly ContextMenuStripItem _pasteItem;
        private readonly ContextMenuStripItem _selectAllItem;
        private readonly ContextMenuStripItem _clearSelectionItem;

        private readonly Label _headerLabel;
        private readonly Label _modeStatusLabel;
        private readonly StandardButton _insertButton;
        private readonly StandardButton _previewAllButton;
        private readonly StandardButton _previewSelectedButton;
        private readonly StandardButton _pauseButton;
        private readonly StandardButton _stopButton;
        private readonly Label _playbackStatusLabel;
        private bool _suppressSectionScroll;
        private readonly StandardButton _undoButton;
        private readonly StandardButton _clearButton;
        private readonly Panel _headerPanel;
        private readonly Panel _footerPanel;
        private readonly FlowPanel _chipsContainer;
        private Panel _bottomSpacer;
        private readonly StandardButton _sectionButton;
        private readonly ContextMenuStrip _sectionMenu;
        private readonly CustomDropdown _sectionJumpDropdown;
        private TextBox _customSectionInput;

        private int _playbackHighlightIndex = -1;
        private HashSet<int> _savedSelection;
        private bool _isPlaybackActive;
        private int[] _playbackMapping;
        private int[] _playbackNoteIndices;
        private SongPlayer _activeSongPlayer;

        public static bool IsSectionMarker(string s) => s != null && s.StartsWith("[") && s.EndsWith("]");
        public static string GetSectionName(string s) => s.Substring(1, s.Length - 2);

        public IReadOnlyList<string> Notes => _notes.AsReadOnly();
        public int NoteCount => _notes.Count(n => !IsSectionMarker(n));
        public bool HasSelection => _selectedIndices.Count > 0;
        public bool IsInsertMode => _isInsertMode;
        public bool IsReplaceMode => _isReplaceMode;
        public int ReplaceTargetIndex => _replaceTargetIndex;

        public NoteSequencePanel(int width, int height)
        {
            Size = new Point(width, height);
            BackgroundColor = Color.Transparent;

            var headerHeight = Layout.HeaderHeight + 58;
            _headerPanel = new Panel
            {
                Parent = this,
                Location = new Point(0, 0),
                Size = new Point(width, headerHeight),
                BackgroundColor = MaestroTheme.SlateGray,
                ShowBorder = true
            };

            _headerLabel = new Label
            {
                Parent = _headerPanel,
                Text = "Notes: 0",
                Location = new Point(Layout.Padding, 4),
                Size = new Point(140, Layout.HeaderHeight - 4),
                Font = GameService.Content.DefaultFont14,
                TextColor = MaestroTheme.CreamWhite
            };

            // Header buttons: right-aligned with equal 5px gaps
            // Clear(55) gap Undo(55) gap Insert(55) gap Section(60)
            const int btnGap = 5;
            const int btnW = 55;
            const int sectionBtnW = 60;
            var clearX = width - 10 - btnW;
            var undoX = clearX - btnGap - btnW;
            var insertX = undoX - btnGap - btnW;
            var sectionX = insertX - btnGap - sectionBtnW;

            _clearButton = new StandardButton
            {
                Parent = _headerPanel,
                Text = "Clear",
                Location = new Point(clearX, Layout.ButtonY),
                Size = new Point(btnW, Layout.HeaderHeight)
            };
            _clearButton.Click += (s, e) =>
            {
                ClearClicked?.Invoke(this, EventArgs.Empty);
                Clear();
            };

            _undoButton = new StandardButton
            {
                Parent = _headerPanel,
                Text = "Undo",
                Location = new Point(undoX, Layout.ButtonY),
                Size = new Point(btnW, Layout.HeaderHeight)
            };
            _undoButton.Click += (s, e) =>
            {
                UndoClicked?.Invoke(this, EventArgs.Empty);
                Undo();
            };

            _sectionMenu = new ContextMenuStrip();
            foreach (var name in new[] { "Intro", "Verse", "Chorus", "Bridge", "Outro" })
            {
                var item = _sectionMenu.AddMenuItem(name);
                var captured = name;
                item.Click += (s, e) => AddSectionMarker(captured);
            }
            var customItem = _sectionMenu.AddMenuItem("Custom...");
            customItem.Click += (s, e) => ShowCustomSectionInput();

            _sectionButton = new StandardButton
            {
                Parent = _headerPanel,
                Text = "Section",
                Location = new Point(sectionX, Layout.ButtonY),
                Size = new Point(sectionBtnW, Layout.HeaderHeight),
                BasicTooltipText = "Add a section marker to organize notes"
            };
            _sectionButton.Click += (s, e) => _sectionMenu.Show(_sectionButton);

            _insertButton = new StandardButton
            {
                Parent = _headerPanel,
                Text = "Insert",
                Location = new Point(insertX, Layout.ButtonY),
                Size = new Point(btnW, Layout.HeaderHeight),
                BasicTooltipText = "When active, new notes insert after the selected note instead of appending to the end"
            };
            _insertButton.Click += (s, e) =>
            {
                _isInsertMode = !_isInsertMode;
                UpdateInsertVisual();
                UpdateModeStatus();
                InsertModeChanged?.Invoke(this, EventArgs.Empty);
            };

            _sectionJumpDropdown = new CustomDropdown
            {
                Parent = this,
                Location = new Point(Layout.Padding, Layout.HeaderHeight + 18),
                Size = new Point(150, 27),
                Visible = false,
                BasicTooltipText = "Jump to section"
            };
            _sectionJumpDropdown.ValueChanged += OnSectionJumpChanged;

            _modeStatusLabel = new Label
            {
                Parent = _headerPanel,
                Text = "",
                Location = new Point(0, Layout.HeaderHeight + 19),
                Size = new Point(width - 10, 18),
                Font = GameService.Content.DefaultFont14,
                TextColor = MaestroTheme.AmberGold,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            const int gapHeight = 4;
            const int footerHeight = 46;
            var chipsY = headerHeight + gapHeight;
            var chipsHeight = height - chipsY - footerHeight - gapHeight;
            _chipsContainer = new FlowPanel
            {
                Parent = this,
                Location = new Point(0, chipsY),
                Size = new Point(width, chipsHeight),
                FlowDirection = ControlFlowDirection.LeftToRight,
                ControlPadding = new Vector2(Layout.ChipSpacing, Layout.ChipSpacing),
                OuterControlPadding = new Vector2(Layout.Padding, Layout.Padding),
                CanScroll = true,
                ShowBorder = false,
                BackgroundColor = Color.Transparent
            };

            // Context menu on the chips container
            _contextMenu = new ContextMenuStrip();
            _previewSelectedItem = _contextMenu.AddMenuItem("Preview Selected");
            _previewSelectedItem.Click += (s, e) => PreviewSelectionRequested?.Invoke(this, EventArgs.Empty);

            _deleteSelectedItem = _contextMenu.AddMenuItem("Delete Selected");
            _deleteSelectedItem.Click += (s, e) => RemoveSelected();

            _replaceItem = _contextMenu.AddMenuItem("Replace");
            _replaceItem.Click += (s, e) => EnterReplaceMode();

            _copyItem = _contextMenu.AddMenuItem("Copy");
            _copyItem.Click += (s, e) => CopySelected();

            _pasteItem = _contextMenu.AddMenuItem("Paste");
            _pasteItem.Click += (s, e) => PasteClipboard();

            _selectAllItem = _contextMenu.AddMenuItem("Select All");
            _selectAllItem.Click += (s, e) => SelectAll();

            _clearSelectionItem = _contextMenu.AddMenuItem("Clear Selection");
            _clearSelectionItem.Click += (s, e) => ClearSelection();

            _chipsContainer.Menu = _contextMenu;

            // Footer panel with playback controls
            _footerPanel = new Panel
            {
                Parent = this,
                Location = new Point(0, height - footerHeight),
                Size = new Point(width, footerHeight),
                BackgroundColor = Color.Transparent
            };

            const int btnHeight = 26;
            const int btnY = 6;

            // Left side: playback controls (hidden until playing)
            _pauseButton = new StandardButton
            {
                Parent = _footerPanel,
                Text = "||",
                Location = new Point(Layout.Padding, btnY),
                Size = new Point(30, btnHeight),
                Enabled = false
            };
            _pauseButton.Click += (s, e) => PauseRequested?.Invoke(this, EventArgs.Empty);

            _stopButton = new StandardButton
            {
                Parent = _footerPanel,
                Text = "X",
                Location = new Point(Layout.Padding + 35, btnY),
                Size = new Point(30, btnHeight),
                Enabled = false
            };
            _stopButton.Click += (s, e) => StopRequested?.Invoke(this, EventArgs.Empty);

            _playbackStatusLabel = new Label
            {
                Parent = _footerPanel,
                Text = "No song playing",
                Location = new Point(Layout.Padding + 72, btnY),
                Size = new Point(200, btnHeight),
                Font = GameService.Content.DefaultFont14,
                TextColor = MaestroTheme.LightGray,
                VerticalAlignment = VerticalAlignment.Middle
            };

            // Right side: preview buttons
            _previewAllButton = new StandardButton
            {
                Parent = _footerPanel,
                Text = "Preview All",
                Location = new Point(width - 95, btnY),
                Size = new Point(85, btnHeight),
                BasicTooltipText = "Preview all notes"
            };
            _previewAllButton.Click += (s, e) => PreviewAllRequested?.Invoke(this, EventArgs.Empty);

            _previewSelectedButton = new StandardButton
            {
                Parent = _footerPanel,
                Text = "Preview Selected",
                Location = new Point(width - 220, btnY),
                Size = new Point(120, btnHeight),
                Enabled = false,
                BasicTooltipText = "Preview selected notes"
            };
            _previewSelectedButton.Click += (s, e) => PreviewSelectionRequested?.Invoke(this, EventArgs.Empty);

            UpdateButtonStates();
        }

        public IReadOnlyList<string> GetSelectedNotes()
        {
            return _selectedIndices.OrderBy(i => i).Select(i => _notes[i]).ToList();
        }

        public IReadOnlyList<int> GetSelectedIndices()
        {
            return _selectedIndices.OrderBy(i => i).ToList();
        }

        public void ClearSelection()
        {
            foreach (var index in _selectedIndices)
            {
                if (index < _chips.Count)
                    _chips[index].IsSelected = false;
            }

            _selectedIndices.Clear();
            _lastClickedIndex = -1;
            UpdateHeader();
            UpdateContextMenuStates();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SelectAll()
        {
            for (var i = 0; i < _chips.Count; i++)
            {
                if (_chips[i] is SectionMarkerChip) continue;
                _selectedIndices.Add(i);
                _chips[i].IsSelected = true;
            }

            UpdateHeader();
            UpdateContextMenuStates();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RemoveSelected()
        {
            if (_selectedIndices.Count == 0) return;

            PushUndo();

            // Remove in descending order to preserve indices
            foreach (var index in _selectedIndices.OrderByDescending(i => i))
            {
                _notes.RemoveAt(index);

                var chip = _chips[index];
                chip.ChipClicked -= OnChipClicked;
                chip.RemoveClicked -= OnChipRemoveClicked;
                chip.Dispose();
                _chips.RemoveAt(index);
            }

            _selectedIndices.Clear();
            _lastClickedIndex = -1;

            // Exit insert mode when selection is lost
            if (_isInsertMode)
                ResetModes();

            UpdateIndices();
            UpdateHeader();
            UpdateButtonStates();
            UpdateContextMenuStates();
            SequenceChanged?.Invoke(this, EventArgs.Empty);
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void AddNote(string noteString)
        {
            PushUndo();
            _notes.Add(noteString);

            var chip = CreateChip(noteString, _notes.Count - 1);
            chip.Parent = _chipsContainer;
            chip.RemoveClicked += OnChipRemoveClicked;
            chip.ChipClicked += OnChipClicked;
            _chips.Add(chip);

            EnsureBottomSpacer();
            RequestScrollTo(_notes.Count - 1);
            UpdateHeader();
            UpdateButtonStates();
            UpdateContextMenuStates();
            SequenceChanged?.Invoke(this, EventArgs.Empty);
        }

        public void InsertAt(int index, string noteString)
        {
            PushUndo();
            index = Math.Max(0, Math.Min(index, _notes.Count));

            _notes.Insert(index, noteString);

            var chip = CreateChip(noteString, index);
            chip.RemoveClicked += OnChipRemoveClicked;
            chip.ChipClicked += OnChipClicked;
            _chips.Insert(index, chip);

            // Shift selected indices that are >= index
            var shifted = new HashSet<int>();
            foreach (var si in _selectedIndices)
                shifted.Add(si >= index ? si + 1 : si);
            _selectedIndices.Clear();
            foreach (var si in shifted)
                _selectedIndices.Add(si);

            if (_lastClickedIndex >= index)
                _lastClickedIndex++;

            ReorderChipsFrom(index);
            UpdateIndices();
            EnsureBottomSpacer();
            RequestScrollTo(index);
            UpdateHeader();
            UpdateButtonStates();
            UpdateContextMenuStates();
            SequenceChanged?.Invoke(this, EventArgs.Empty);
        }

        public void InsertRange(int index, IList<string> noteStrings)
        {
            if (noteStrings.Count == 0) return;
            PushUndo();
            index = Math.Max(0, Math.Min(index, _notes.Count));

            for (int i = 0; i < noteStrings.Count; i++)
            {
                var insertIdx = index + i;
                _notes.Insert(insertIdx, noteStrings[i]);

                var chip = CreateChip(noteStrings[i], insertIdx);
                chip.RemoveClicked += OnChipRemoveClicked;
                chip.ChipClicked += OnChipClicked;
                _chips.Insert(insertIdx, chip);
            }

            // Shift selected indices in bulk
            var shifted = new HashSet<int>();
            foreach (var si in _selectedIndices)
                shifted.Add(si >= index ? si + noteStrings.Count : si);
            _selectedIndices.Clear();
            foreach (var si in shifted)
                _selectedIndices.Add(si);

            if (_lastClickedIndex >= index)
                _lastClickedIndex += noteStrings.Count;

            ReorderChipsFrom(index);
            UpdateIndices();
            EnsureBottomSpacer();
            RequestScrollTo(index);
            UpdateHeader();
            UpdateButtonStates();
            UpdateContextMenuStates();
            SequenceChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ReplaceAt(int index, string noteString)
        {
            if (index < 0 || index >= _notes.Count) return;

            PushUndo();
            _notes[index] = noteString;

            var oldChip = _chips[index];
            oldChip.ChipClicked -= OnChipClicked;
            oldChip.RemoveClicked -= OnChipRemoveClicked;
            oldChip.Parent = null;
            oldChip.Dispose();

            var newChip = CreateChip(noteString, index);
            newChip.RemoveClicked += OnChipRemoveClicked;
            newChip.ChipClicked += OnChipClicked;
            _chips[index] = newChip;

            ReorderChipsFrom(index);
            newChip.IsSelected = _selectedIndices.Contains(index);

            RequestScrollTo(index);
            UpdateHeader();
            SequenceChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SelectSingle(int index)
        {
            if (index < 0 || index >= _chips.Count) return;

            foreach (var si in _selectedIndices.Where(si => si < _chips.Count))
                _chips[si].IsSelected = false;
            _selectedIndices.Clear();

            _selectedIndices.Add(index);
            _chips[index].IsSelected = true;
            _lastClickedIndex = index;

            UpdateHeader();
            UpdateContextMenuStates();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SelectRange(int from, int to)
        {
            if (from < 0) from = 0;

            foreach (var si in _selectedIndices.Where(si => si < _chips.Count))
                _chips[si].IsSelected = false;
            _selectedIndices.Clear();

            for (var i = from; i <= to && i < _chips.Count; i++)
            {
                _selectedIndices.Add(i);
                _chips[i].IsSelected = true;
            }

            _lastClickedIndex = to < _chips.Count ? to : -1;

            UpdateHeader();
            UpdateContextMenuStates();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public int GetLastSelectedIndex()
        {
            return _selectedIndices.Count > 0 ? _selectedIndices.Max() : -1;
        }

        public void EnterReplaceMode()
        {
            if (_selectedIndices.Count != 1) return;
            _isReplaceMode = true;
            _replaceTargetIndex = _selectedIndices.First();
            UpdateModeStatus();
            ReplaceModeChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ExitReplaceMode()
        {
            if (!_isReplaceMode) return;
            _isReplaceMode = false;
            _replaceTargetIndex = -1;
            UpdateModeStatus();
            ReplaceModeChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ResetModes()
        {
            if (_isInsertMode)
            {
                _isInsertMode = false;
                UpdateInsertVisual();
                InsertModeChanged?.Invoke(this, EventArgs.Empty);
            }

            ExitReplaceMode();
            UpdateModeStatus();
        }

        private void UpdateInsertVisual()
        {
            _insertButton.BackgroundColor = _isInsertMode ? MaestroTheme.AmberGold : Color.Transparent;
        }

        public void CopySelected()
        {
            if (_selectedIndices.Count == 0) return;
            _clipboard.Clear();
            _clipboard.AddRange(_selectedIndices.OrderBy(i => i).Select(i => _notes[i]));
            UpdateContextMenuStates();
        }

        public void PasteClipboard()
        {
            if (_clipboard.Count == 0) return;

            var insertIndex = _selectedIndices.Count > 0
                ? _selectedIndices.Max() + 1
                : _notes.Count;

            InsertRange(insertIndex, _clipboard);
            SelectRange(insertIndex, insertIndex + _clipboard.Count - 1);
        }

        public void ResizeTo(int width, int height)
        {
            Size = new Point(width, height);

            // Resize header panel
            var headerHeight = Layout.HeaderHeight + 58;
            _headerPanel.Size = new Point(width, headerHeight);

            // Reposition right-aligned header buttons with equal gaps
            const int btnGap = 5;
            const int btnW = 55;
            const int sectionBtnW = 60;
            var clearX = width - 10 - btnW;
            var undoX = clearX - btnGap - btnW;
            var insertX = undoX - btnGap - btnW;
            var sectionX = insertX - btnGap - sectionBtnW;
            _clearButton.Location = new Point(clearX, Layout.ButtonY);
            _undoButton.Location = new Point(undoX, Layout.ButtonY);
            _insertButton.Location = new Point(insertX, Layout.ButtonY);
            _sectionButton.Location = new Point(sectionX, Layout.ButtonY);

            // Update mode status label width
            _modeStatusLabel.Size = new Point(width - 10, 18);

            // Resize chips container and footer
            const int gapHeight = 4;
            const int footerHeight = 46;
            var chipsY = headerHeight + gapHeight;
            var chipsHeight = height - chipsY - footerHeight - gapHeight;
            _chipsContainer.Location = new Point(0, chipsY);
            _chipsContainer.Size = new Point(width, chipsHeight);

            _footerPanel.Location = new Point(0, height - footerHeight);
            _footerPanel.Size = new Point(width, footerHeight);
            _previewAllButton.Location = new Point(width - 95, _previewAllButton.Location.Y);
            _previewSelectedButton.Location = new Point(width - 220, _previewSelectedButton.Location.Y);

            // Resize section marker chips to match new width
            foreach (var chip in _chips)
            {
                if (chip is SectionMarkerChip)
                    chip.Size = new Point(width - 26, SectionMarkerChip.Layout.Height);
            }
        }

        public override void UpdateContainer(GameTime gameTime)
        {
            base.UpdateContainer(gameTime);

            if (_pendingScrollToIndex >= 0 && _scrollApplyFrames > 0)
            {
                _scrollApplyFrames--;
                ScrollToChip(_pendingScrollToIndex);

                if (_scrollApplyFrames <= 0)
                    _pendingScrollToIndex = -1;
            }

            if (_isPlaybackActive)
            {
                UpdatePlaybackHighlight();
            }
        }


        private void ScrollToChip(int chipIndex)
        {
            if (chipIndex < 0 || chipIndex >= _chips.Count) return;

            var scrollbar = GetScrollbar();
            if (scrollbar == null) return;

            var chip = _chips[chipIndex];
            var chipBottom = chip.Location.Y + chip.Height;
            var viewportHeight = _chipsContainer.ContentRegion.Height;

            // Find the lowest visible child to determine total content height
            var contentHeight = 0;
            foreach (var child in _chipsContainer.Children)
            {
                if (child.Visible && child.Bottom > contentHeight)
                    contentHeight = child.Bottom;
            }

            contentHeight = Math.Max(contentHeight, viewportHeight);
            var scrollableRange = contentHeight - viewportHeight;

            if (scrollableRange <= 0) return;

            // Calculate the scroll distance (0-1) needed to make the chip visible
            var targetOffset = chipBottom - viewportHeight + Layout.PaddingBottom;
            targetOffset = Math.Max(0, Math.Min(targetOffset, scrollableRange));

            var scrollDistance = (float)targetOffset / scrollableRange;
            scrollbar.ScrollDistance = Math.Max(0f, Math.Min(1f, scrollDistance));
        }

        private void ReorderChipsFrom(int startIndex)
        {
            var scrollbar = GetScrollbar();
            var savedDistance = scrollbar?.ScrollDistance ?? 0f;

            if (_bottomSpacer != null)
                _bottomSpacer.Parent = null;

            for (int i = startIndex; i < _chips.Count; i++)
                _chips[i].Parent = null;
            for (int i = startIndex; i < _chips.Count; i++)
                _chips[i].Parent = _chipsContainer;

            if (_bottomSpacer != null)
                _bottomSpacer.Parent = _chipsContainer;

            if (scrollbar != null)
                scrollbar.ScrollDistance = savedDistance;
        }

        private const int MaxUndoDepth = 100;

        private void PushUndo()
        {
            if (_undoStack.Count >= MaxUndoDepth)
            {
                var items = _undoStack.ToArray();
                _undoStack.Clear();
                // Rebuild stack without the oldest entry (last in array)
                for (var i = items.Length - 2; i >= 0; i--)
                    _undoStack.Push(items[i]);
            }

            _undoStack.Push(new List<string>(_notes));
        }

        public void Undo()
        {
            if (_undoStack.Count == 0) return;

            var previousState = _undoStack.Pop();
            RestoreFromNotes(previousState);
            SequenceChanged?.Invoke(this, EventArgs.Empty);
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ClearUndoStack()
        {
            _undoStack.Clear();
            UpdateButtonStates();
        }

        private void RestoreFromNotes(List<string> notes)
        {
            foreach (var chip in _chips)
            {
                chip.ChipClicked -= OnChipClicked;
                chip.RemoveClicked -= OnChipRemoveClicked;
                chip.Dispose();
            }

            _chips.Clear();
            _notes.Clear();
            _selectedIndices.Clear();
            _lastClickedIndex = -1;
            _bottomSpacer?.Dispose();
            _bottomSpacer = null;

            foreach (var note in notes)
            {
                _notes.Add(note);
                var chip = CreateChip(note, _notes.Count - 1);
                chip.Parent = _chipsContainer;
                chip.RemoveClicked += OnChipRemoveClicked;
                chip.ChipClicked += OnChipClicked;
                _chips.Add(chip);
            }

            if (_notes.Count > 0)
                EnsureBottomSpacer();

            UpdateIndices();
            UpdateHeader();
            UpdateButtonStates();
            UpdateContextMenuStates();
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _notes.Count) return;

            PushUndo();
            _notes.RemoveAt(index);

            var chip = _chips[index];
            chip.ChipClicked -= OnChipClicked;
            chip.RemoveClicked -= OnChipRemoveClicked;
            chip.Dispose();
            _chips.RemoveAt(index);

            // Update selection: remove the index and shift higher indices down
            _selectedIndices.Remove(index);
            var shifted = new HashSet<int>();
            foreach (var si in _selectedIndices)
            {
                shifted.Add(si > index ? si - 1 : si);
            }

            _selectedIndices.Clear();
            foreach (var si in shifted)
            {
                _selectedIndices.Add(si);
            }

            // Adjust anchor
            if (_lastClickedIndex == index)
                _lastClickedIndex = -1;
            else if (_lastClickedIndex > index)
                _lastClickedIndex--;

            // Exit insert mode when selection is lost
            if (_isInsertMode && _selectedIndices.Count == 0)
                ResetModes();

            UpdateIndices();
            UpdateHeader();
            UpdateButtonStates();
            UpdateContextMenuStates();
            SequenceChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Clear()
        {
            PushUndo();
            _notes.Clear();
            _selectedIndices.Clear();
            _lastClickedIndex = -1;

            foreach (var chip in _chips)
            {
                chip.ChipClicked -= OnChipClicked;
                chip.RemoveClicked -= OnChipRemoveClicked;
                chip.Dispose();
            }

            _chips.Clear();

            _bottomSpacer?.Dispose();
            _bottomSpacer = null;

            UpdateHeader();
            UpdateButtonStates();
            UpdateContextMenuStates();
            SequenceChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnChipClicked(object sender, MouseEventArgs e)
        {
            if (!(sender is BaseChip chip)) return;
            if (chip is SectionMarkerChip) return;

            var index = chip.Index;
            var modifiers = GameService.Input.Keyboard.ActiveModifiers;
            var isShift = modifiers.HasFlag(ModifierKeys.Shift);
            var isCtrl = modifiers.HasFlag(ModifierKeys.Ctrl);

            if (isShift && _lastClickedIndex >= 0)
            {
                // Range select from anchor to clicked
                var from = Math.Min(_lastClickedIndex, index);
                var to = Math.Max(_lastClickedIndex, index);

                // Clear previous selection unless Ctrl is also held
                if (!isCtrl)
                {
                    foreach (var si in _selectedIndices)
                    {
                        if (si < _chips.Count)
                            _chips[si].IsSelected = false;
                    }

                    _selectedIndices.Clear();
                }

                for (var i = from; i <= to; i++)
                {
                    if (_chips[i] is SectionMarkerChip) continue;
                    _selectedIndices.Add(i);
                    _chips[i].IsSelected = true;
                }
            }
            else if (isCtrl)
            {
                // Toggle individual chip
                if (!_selectedIndices.Add(index))
                {
                    _selectedIndices.Remove(index);
                    chip.IsSelected = false;
                }
                else
                {
                    chip.IsSelected = true;
                }

                _lastClickedIndex = index;
            }
            else
            {
                // Plain click: toggle if clicking the only selected note, otherwise select only this
                var wasOnlySelected = _selectedIndices.Count == 1 && _selectedIndices.Contains(index);

                foreach (var si in _selectedIndices.Where(si => si < _chips.Count))
                    _chips[si].IsSelected = false;
                _selectedIndices.Clear();

                if (!wasOnlySelected)
                {
                    _selectedIndices.Add(index);
                    chip.IsSelected = true;
                    _lastClickedIndex = index;
                }
                else
                {
                    _lastClickedIndex = -1;
                }
            }

            // Auto-exit replace mode if selection changes
            if (_isReplaceMode && (_selectedIndices.Count != 1 || !_selectedIndices.Contains(_replaceTargetIndex)))
                ExitReplaceMode();

            UpdateHeader();
            UpdateContextMenuStates();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnChipRemoveClicked(object sender, EventArgs e)
        {
            if (sender is BaseChip chip)
            {
                RemoveAt(chip.Index);
            }
        }

        private void UpdateIndices()
        {
            for (int i = 0; i < _chips.Count; i++)
            {
                _chips[i].Index = i;
            }
        }

        private void UpdateHeader()
        {
            var noteCount = _notes.Count(n => !IsSectionMarker(n));
            _headerLabel.Text = _selectedIndices.Count > 0
                ? $"Notes: {noteCount} ({_selectedIndices.Count} selected)"
                : $"Notes: {noteCount}";
            UpdateModeStatus();
            UpdateSectionDropdown();
        }

        private void UpdateModeStatus()
        {
            if (_isReplaceMode)
            {
                _modeStatusLabel.Text = $"Click a key to replace note #{_replaceTargetIndex + 1}";
                _modeStatusLabel.TextColor = MaestroTheme.Error;
            }
            else if (_isInsertMode && _selectedIndices.Count > 0)
            {
                _modeStatusLabel.Text = $"Inserting after note #{_selectedIndices.Max() + 1}";
                _modeStatusLabel.TextColor = MaestroTheme.AmberGold;
            }
            else if (_isInsertMode)
            {
                _modeStatusLabel.Text = "Select a note to insert after";
                _modeStatusLabel.TextColor = MaestroTheme.AmberGold;
            }
            else if (_chips.Count > 0)
            {
                _modeStatusLabel.Text = "Right-click notes for more options";
                _modeStatusLabel.TextColor = MaestroTheme.LightGray;
            }
            else
            {
                _modeStatusLabel.Text = "";
            }
        }

        private void UpdateButtonStates()
        {
            _undoButton.Enabled = _undoStack.Count > 0;
            _clearButton.Enabled = _notes.Count > 0;
        }

        private void UpdateContextMenuStates()
        {
            var hasSelection = _selectedIndices.Count > 0;
            var hasChips = _chips.Count > 0;

            _previewSelectedItem.Enabled = hasSelection;
            _previewSelectedButton.Enabled = hasSelection;
            _deleteSelectedItem.Enabled = hasSelection;
            _replaceItem.Enabled = _selectedIndices.Count == 1;
            _copyItem.Enabled = hasSelection;
            _pasteItem.Enabled = _clipboard.Count > 0;
            _selectAllItem.Enabled = hasChips;
            _clearSelectionItem.Enabled = hasSelection;
        }

        private void EnsureBottomSpacer()
        {
            _bottomSpacer?.Dispose();
            _bottomSpacer = new Panel
            {
                Parent = _chipsContainer,
                Size = new Point(_chipsContainer.Width, Layout.PaddingBottom),
                BackgroundColor = Color.Transparent
            };
        }

        public void SetControlsEnabled(bool enabled)
        {
            // Footer preview buttons
            _previewAllButton.Enabled = enabled;
            _previewSelectedButton.Enabled = enabled && _selectedIndices.Count > 0;

            // Playback controls
            _pauseButton.Enabled = !enabled;
            _stopButton.Enabled = !enabled;
            if (!enabled)
            {
                _playbackStatusLabel.Text = "Playing...";
                _playbackStatusLabel.TextColor = MaestroTheme.Playing;
                _pauseButton.Text = "||";
            }
            else
            {
                _playbackStatusLabel.Text = "No song playing";
                _playbackStatusLabel.TextColor = MaestroTheme.LightGray;
                _pauseButton.Text = "||";
            }

            // Header buttons
            _sectionButton.Enabled = enabled;
            _insertButton.Enabled = enabled;
            _undoButton.Enabled = enabled;
            _clearButton.Enabled = enabled;

            // Context menu
            _previewSelectedItem.Enabled = enabled && _selectedIndices.Count > 0;
            _deleteSelectedItem.Enabled = enabled && _selectedIndices.Count > 0;
            _replaceItem.Enabled = enabled && _selectedIndices.Count == 1;
            _copyItem.Enabled = enabled && _selectedIndices.Count > 0;
            _pasteItem.Enabled = enabled && _clipboard.Count > 0;
            _selectAllItem.Enabled = enabled;
            _clearSelectionItem.Enabled = enabled && _selectedIndices.Count > 0;
        }

        public void SetPlaybackPaused(bool paused)
        {
            _pauseButton.Text = paused ? ">" : "||";
            _playbackStatusLabel.Text = paused ? "Paused" : "Playing...";
            _playbackStatusLabel.TextColor = paused ? MaestroTheme.Paused : MaestroTheme.Playing;
        }

        public void StartPlaybackHighlight(SongPlayer player, int[] mapping, int[] noteIndices)
        {
            _savedSelection = new HashSet<int>(_selectedIndices);
            foreach (var si in _selectedIndices)
            {
                if (si < _chips.Count)
                    _chips[si].IsSelected = false;
            }
            _selectedIndices.Clear();
            _playbackHighlightIndex = -1;
            _playbackMapping = mapping;
            _playbackNoteIndices = noteIndices;
            _activeSongPlayer = player;
            _isPlaybackActive = true;
        }

        public void StopPlaybackHighlight()
        {
            _isPlaybackActive = false;
            _playbackMapping = null;
            _playbackNoteIndices = null;
            _activeSongPlayer = null;

            if (_playbackHighlightIndex >= 0 && _playbackHighlightIndex < _chips.Count)
                _chips[_playbackHighlightIndex].IsSelected = false;
            _playbackHighlightIndex = -1;

            if (_savedSelection != null)
            {
                foreach (var si in _savedSelection)
                {
                    if (si < _chips.Count)
                    {
                        _selectedIndices.Add(si);
                        _chips[si].IsSelected = true;
                    }
                }
                _savedSelection = null;
            }

            UpdateHeader();
            UpdateContextMenuStates();
        }

        private void UpdatePlaybackHighlight()
        {
            if (_activeSongPlayer == null || !_activeSongPlayer.IsPlaying) return;
            var player = _activeSongPlayer;

            if (player.IsAdjustingOctave)
            {
                _playbackStatusLabel.Text = "Adjusting...";
                _playbackStatusLabel.TextColor = MaestroTheme.AmberGold;
                return;
            }

            if (player.IsPaused)
                return;

            _playbackStatusLabel.Text = "Playing...";
            _playbackStatusLabel.TextColor = MaestroTheme.Playing;

            var cmdIndex = player.CurrentCommandIndex;
            if (_playbackMapping == null || cmdIndex >= _playbackMapping.Length) return;

            var noteLineIndex = _playbackMapping[cmdIndex];

            if (_playbackNoteIndices != null && noteLineIndex < _playbackNoteIndices.Length)
                noteLineIndex = _playbackNoteIndices[noteLineIndex];

            if (noteLineIndex == _playbackHighlightIndex) return;

            if (_playbackHighlightIndex >= 0 && _playbackHighlightIndex < _chips.Count)
                _chips[_playbackHighlightIndex].IsSelected = false;

            _playbackHighlightIndex = noteLineIndex;

            if (noteLineIndex >= 0 && noteLineIndex < _chips.Count && !(_chips[noteLineIndex] is SectionMarkerChip))
            {
                _chips[noteLineIndex].IsSelected = true;
                RequestScrollTo(noteLineIndex);
            }
        }

        private BaseChip CreateChip(string noteString, int index)
        {
            if (IsSectionMarker(noteString))
            {
                return new SectionMarkerChip(GetSectionName(noteString), index, _chipsContainer.Width);
            }

            return new NoteChip(noteString, index);
        }

        private void AddSectionMarker(string name)
        {
            var marker = $"[{name}]";

            if (_isInsertMode && _selectedIndices.Count > 0)
            {
                var insertIndex = _selectedIndices.Max() + 1;
                InsertAt(insertIndex, marker);
                SelectSingle(insertIndex);
            }
            else
            {
                AddNote(marker);
            }

            // Select the newly added section in the jump dropdown without scrolling
            _suppressSectionScroll = true;
            for (int i = 0; i < _sectionJumpDropdown.ItemCount; i++)
            {
                if (_sectionJumpDropdown.ItemAt(i)?.DisplayText == name)
                {
                    _sectionJumpDropdown.SelectedIndex = i;
                    break;
                }
            }
            _suppressSectionScroll = false;
        }

        private void ShowCustomSectionInput()
        {
            if (_customSectionInput != null) return;

            // Place side by side with the dropdown
            var inputX = _sectionJumpDropdown.Visible
                ? _sectionJumpDropdown.Location.X + _sectionJumpDropdown.Width + 5
                : Layout.Padding;
            var inputY = _sectionJumpDropdown.Visible
                ? _sectionJumpDropdown.Location.Y
                : _modeStatusLabel.Location.Y;

            _customSectionInput = new TextBox
            {
                Parent = this,
                Location = new Point(inputX, inputY),
                Width = 140,
                PlaceholderText = "Section name...",
                ZIndex = 50
            };
            _customSectionInput.EnterPressed += (s, e) =>
            {
                var name = ((TextBox)s).Text.Trim();
                RemoveCustomSectionInput();
                if (!string.IsNullOrEmpty(name))
                    AddSectionMarker(name);
            };
            _modeStatusLabel.Visible = false;
        }

        private void RemoveCustomSectionInput()
        {
            if (_customSectionInput == null) return;
            _customSectionInput.Dispose();
            _customSectionInput = null;
            _modeStatusLabel.Visible = true;
        }

        private void OnSectionJumpChanged(object sender, ValueChangedEventArgs e)
        {
            if (_suppressSectionScroll) return;
            ScrollToSection(e.CurrentValue);
        }

        private void ScrollToSection(string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName)) return;

            for (int i = 0; i < _notes.Count; i++)
            {
                if (IsSectionMarker(_notes[i]) && GetSectionName(_notes[i]) == sectionName)
                {
                    RequestScrollTo(i);
                    break;
                }
            }
        }

        private void UpdateSectionDropdown()
        {
            var previousSelection = _sectionJumpDropdown.SelectedValue;

            var sections = new List<string>();
            foreach (var note in _notes)
            {
                if (IsSectionMarker(note))
                    sections.Add(GetSectionName(note));
            }

            // Suppress scrolling during entire rebuild
            _suppressSectionScroll = true;

            _sectionJumpDropdown.ClearItems();
            foreach (var section in sections)
                _sectionJumpDropdown.AddItem(section);

            _sectionJumpDropdown.Visible = sections.Count > 0;

            // Restore previous selection
            if (previousSelection != null)
            {
                for (int i = 0; i < _sectionJumpDropdown.ItemCount; i++)
                {
                    if (_sectionJumpDropdown.ItemAt(i)?.DisplayText == previousSelection)
                    {
                        _sectionJumpDropdown.SelectedIndex = i;
                        break;
                    }
                }
            }

            _suppressSectionScroll = false;
        }

        protected override void DisposeControl()
        {
            _headerPanel?.Dispose();
            _footerPanel?.Dispose();
            _headerLabel?.Dispose();
            _modeStatusLabel?.Dispose();
            _previewAllButton?.Dispose();
            _previewSelectedButton?.Dispose();
            _pauseButton?.Dispose();
            _stopButton?.Dispose();
            _playbackStatusLabel?.Dispose();
            _sectionButton?.Dispose();
            _sectionMenu?.Dispose();
            _sectionJumpDropdown?.Dispose();
            _customSectionInput?.Dispose();
            _insertButton?.Dispose();
            _undoButton?.Dispose();
            _clearButton?.Dispose();
            _bottomSpacer?.Dispose();

            foreach (var chip in _chips)
            {
                chip.ChipClicked -= OnChipClicked;
                chip.RemoveClicked -= OnChipRemoveClicked;
                chip.Dispose();
            }

            _chips.Clear();

            _contextMenu?.Dispose();
            _chipsContainer?.Dispose();
            base.DisposeControl();
        }
    }
}