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

        private readonly List<string> _notes = new List<string>();
        private readonly List<NoteChip> _chips = new List<NoteChip>();

        private readonly HashSet<int> _selectedIndices = new HashSet<int>();
        private int _lastClickedIndex = -1;

        private readonly ContextMenuStrip _contextMenu;
        private readonly ContextMenuStripItem _previewSelectedItem;
        private readonly ContextMenuStripItem _deleteSelectedItem;
        private readonly ContextMenuStripItem _selectAllItem;
        private readonly ContextMenuStripItem _clearSelectionItem;

        private readonly Label _headerLabel;
        private readonly StandardButton _undoButton;
        private readonly StandardButton _clearButton;
        private readonly FlowPanel _chipsContainer;
        private Panel _bottomSpacer;

        public IReadOnlyList<string> Notes => _notes.AsReadOnly();
        public int NoteCount => _notes.Count;
        public bool HasSelection => _selectedIndices.Count > 0;

        public NoteSequencePanel(int width, int height)
        {
            Size = new Point(width, height);
            BackgroundColor = MaestroTheme.DarkCharcoal;

            _headerLabel = new Label
            {
                Parent = this,
                Text = "Notes: 0",
                Location = new Point(Layout.Padding, 4),
                Size = new Point(200, Layout.HeaderHeight - 4),
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
                RemoveLast();
            };

            var chipsY = Layout.HeaderHeight + Layout.HeaderBottomSpacing;
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

            UpdateIndices();
            UpdateHeader();
            UpdateButtonStates();
            UpdateContextMenuStates();
            SequenceChanged?.Invoke(this, EventArgs.Empty);
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void AddNote(string noteString)
        {
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

        public void RemoveLast()
        {
            if (_notes.Count == 0) return;

            var lastIndex = _notes.Count - 1;

            // Clean up selection for removed chip
            _selectedIndices.Remove(lastIndex);
            if (_lastClickedIndex == lastIndex)
                _lastClickedIndex = -1;

            _notes.RemoveAt(lastIndex);

            var lastChip = _chips[lastIndex];
            lastChip.ChipClicked -= OnChipClicked;
            lastChip.RemoveClicked -= OnChipRemoveClicked;
            lastChip.Dispose();
            _chips.RemoveAt(lastIndex);

            UpdateIndices();
            UpdateHeader();
            UpdateButtonStates();
            UpdateContextMenuStates();
            SequenceChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _notes.Count) return;

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

            UpdateIndices();
            UpdateHeader();
            UpdateButtonStates();
            UpdateContextMenuStates();
            SequenceChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Clear()
        {
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
        }

        private void UpdateButtonStates()
        {
            var hasNotes = _notes.Count > 0;
            _undoButton.Enabled = hasNotes;
            _clearButton.Enabled = hasNotes;
        }

        private void UpdateContextMenuStates()
        {
            var hasSelection = _selectedIndices.Count > 0;
            var hasChips = _chips.Count > 0;

            _previewSelectedItem.Enabled = hasSelection;
            _deleteSelectedItem.Enabled = hasSelection;
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