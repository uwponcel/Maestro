using System;
using System.Collections.Generic;
using System.Linq;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
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
        public event EventHandler PreviewSelectionRequested;
        public event EventHandler InsertModeChanged;
        public event EventHandler ReplaceModeChanged;
        public event EventHandler ExpandRequested;

        private readonly List<string> _notes = new List<string>();
        private readonly List<NoteChip> _chips = new List<NoteChip>();

        private readonly HashSet<int> _selectedIndices = new HashSet<int>();
        private int _lastClickedIndex = -1;

        private bool _isInsertMode;
        private bool _isReplaceMode;
        private int _replaceTargetIndex = -1;
        private readonly List<string> _clipboard = new List<string>();
        private readonly Stack<List<string>> _undoStack = new Stack<List<string>>();

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
        private readonly StandardButton _expandButton;
        private readonly StandardButton _insertButton;
        private readonly StandardButton _undoButton;
        private readonly StandardButton _clearButton;
        private readonly FlowPanel _chipsContainer;
        private Panel _bottomSpacer;

        public IReadOnlyList<string> Notes => _notes.AsReadOnly();
        public int NoteCount => _notes.Count;
        public bool HasSelection => _selectedIndices.Count > 0;
        public bool IsInsertMode => _isInsertMode;
        public bool IsReplaceMode => _isReplaceMode;
        public int ReplaceTargetIndex => _replaceTargetIndex;

        public NoteSequencePanel(int width, int height)
        {
            Size = new Point(width, height);
            BackgroundColor = MaestroTheme.DarkCharcoal;

            _headerLabel = new Label
            {
                Parent = this,
                Text = "Notes: 0",
                Location = new Point(Layout.Padding, 4),
                Size = new Point(120, Layout.HeaderHeight - 4),
                Font = GameService.Content.DefaultFont12,
                TextColor = MaestroTheme.CreamWhite
            };

            _clearButton = new StandardButton
            {
                Parent = this,
                Text = "Clear",
                Location = new Point(width - 65, Layout.ButtonY),
                Size = new Point(55, Layout.HeaderHeight)
            };
            _clearButton.Click += (s, e) =>
            {
                ClearClicked?.Invoke(this, EventArgs.Empty);
                Clear();
            };

            _undoButton = new StandardButton
            {
                Parent = this,
                Text = "Undo",
                Location = new Point(width - 125, Layout.ButtonY),
                Size = new Point(55, Layout.HeaderHeight)
            };
            _undoButton.Click += (s, e) =>
            {
                UndoClicked?.Invoke(this, EventArgs.Empty);
                Undo();
            };

            _insertButton = new StandardButton
            {
                Parent = this,
                Text = "Insert",
                Location = new Point(width - 185, Layout.ButtonY),
                Size = new Point(55, Layout.HeaderHeight),
                BasicTooltipText = "When active, new notes insert after the selected note instead of appending to the end"
            };
            _insertButton.Click += (s, e) =>
            {
                _isInsertMode = !_isInsertMode;
                UpdateInsertVisual();
                UpdateModeStatus();
                InsertModeChanged?.Invoke(this, EventArgs.Empty);
            };

            _expandButton = new StandardButton
            {
                Parent = this,
                Text = "Expand",
                Location = new Point(width - 255, Layout.ButtonY),
                Size = new Point(65, Layout.HeaderHeight),
                BasicTooltipText = "Open notes in a resizable window"
            };
            _expandButton.Click += (s, e) => ExpandRequested?.Invoke(this, EventArgs.Empty);

            _modeStatusLabel = new Label
            {
                Parent = this,
                Text = "",
                Location = new Point(Layout.Padding, Layout.HeaderHeight + 2),
                Size = new Point(width - Layout.Padding * 2, 16),
                Font = GameService.Content.DefaultFont12,
                TextColor = MaestroTheme.AmberGold
            };

            const int chipsY = Layout.HeaderHeight + 18 + Layout.ChipSpacing;
            _chipsContainer = new FlowPanel
            {
                Parent = this,
                Location = new Point(0, chipsY),
                Size = new Point(width, height - chipsY),
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

            UpdateButtonStates();
        }

        public IReadOnlyList<string> GetSelectedNotes()
        {
            return _selectedIndices.OrderBy(i => i).Select(i => _notes[i]).ToList();
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

            var chip = new NoteChip(noteString, _notes.Count - 1)
            {
                Parent = _chipsContainer
            };
            chip.RemoveClicked += OnChipRemoveClicked;
            chip.ChipClicked += OnChipClicked;
            _chips.Add(chip);

            EnsureBottomSpacer();
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

            var chip = new NoteChip(noteString, index);
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

                var chip = new NoteChip(noteStrings[i], insertIdx);
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

            var newChip = new NoteChip(noteString, index);
            newChip.RemoveClicked += OnChipRemoveClicked;
            newChip.ChipClicked += OnChipClicked;
            _chips[index] = newChip;

            ReorderChipsFrom(index);
            newChip.IsSelected = _selectedIndices.Contains(index);

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

            // Reposition right-aligned header buttons
            _expandButton.Location = new Point(width - 255, Layout.ButtonY);
            _insertButton.Location = new Point(width - 185, Layout.ButtonY);
            _undoButton.Location = new Point(width - 125, Layout.ButtonY);
            _clearButton.Location = new Point(width - 65, Layout.ButtonY);

            // Update mode status label width
            _modeStatusLabel.Size = new Point(width - Layout.Padding * 2, 16);

            // Resize chips container
            var chipsY = Layout.HeaderHeight + 18 + Layout.ChipSpacing;
            _chipsContainer.Location = new Point(0, chipsY);
            _chipsContainer.Size = new Point(width, height - chipsY);
        }

        public void SetExpanded(bool expanded)
        {
            _expandButton.Text = expanded ? "Collapse" : "Expand";
            _expandButton.BasicTooltipText = expanded
                ? "Return notes to the creator window"
                : "Open notes in a resizable window";
        }

        private void ReorderChipsFrom(int startIndex)
        {
            if (_bottomSpacer != null)
                _bottomSpacer.Parent = null;

            for (int i = startIndex; i < _chips.Count; i++)
                _chips[i].Parent = null;
            for (int i = startIndex; i < _chips.Count; i++)
                _chips[i].Parent = _chipsContainer;

            if (_bottomSpacer != null)
                _bottomSpacer.Parent = _chipsContainer;
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
                var chip = new NoteChip(note, _notes.Count - 1) { Parent = _chipsContainer };
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
            if (!(sender is NoteChip chip)) return;

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
                // Plain click: clear all, select only this
                foreach (var si in _selectedIndices.Where(si => si < _chips.Count))
                {
                    _chips[si].IsSelected = false;
                }

                _selectedIndices.Clear();

                _selectedIndices.Add(index);
                chip.IsSelected = true;
                _lastClickedIndex = index;
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
            if (sender is NoteChip chip)
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
            _headerLabel.Text = _selectedIndices.Count > 0
                ? $"Notes: {_notes.Count} ({_selectedIndices.Count} selected)"
                : $"Notes: {_notes.Count}";
            UpdateModeStatus();
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

        protected override void DisposeControl()
        {
            _headerLabel?.Dispose();
            _modeStatusLabel?.Dispose();
            _expandButton?.Dispose();
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