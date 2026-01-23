using System;
using System.Collections.Generic;
using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;

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

        private readonly List<string> _notes = new List<string>();
        private readonly List<NoteChip> _chips = new List<NoteChip>();

        private readonly Label _headerLabel;
        private readonly StandardButton _undoButton;
        private readonly StandardButton _clearButton;
        private readonly FlowPanel _chipsContainer;
        private Panel _bottomSpacer;

        public IReadOnlyList<string> Notes => _notes.AsReadOnly();
        public int NoteCount => _notes.Count;

        public NoteSequencePanel(int width, int height)
        {
            Size = new Point(width, height);
            BackgroundColor = MaestroTheme.DarkCharcoal;

            _headerLabel = new Label
            {
                Parent = this,
                Text = "Notes: 0",
                Location = new Point(Layout.Padding, 4),
                Size = new Point(100, Layout.HeaderHeight - 4),
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

            UpdateButtonStates();
        }

        public void AddNote(string noteString)
        {
            _notes.Add(noteString);

            var chip = new NoteChip(noteString, _notes.Count - 1)
            {
                Parent = _chipsContainer
            };
            chip.RemoveClicked += OnChipRemoveClicked;
            _chips.Add(chip);

            EnsureBottomSpacer();
            UpdateHeader();
            UpdateButtonStates();
            SequenceChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RemoveLast()
        {
            if (_notes.Count == 0) return;

            _notes.RemoveAt(_notes.Count - 1);

            var lastChip = _chips[_chips.Count - 1];
            lastChip.RemoveClicked -= OnChipRemoveClicked;
            lastChip.Dispose();
            _chips.RemoveAt(_chips.Count - 1);

            UpdateIndices();
            UpdateHeader();
            UpdateButtonStates();
            SequenceChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _notes.Count) return;

            _notes.RemoveAt(index);

            var chip = _chips[index];
            chip.RemoveClicked -= OnChipRemoveClicked;
            chip.Dispose();
            _chips.RemoveAt(index);

            UpdateIndices();
            UpdateHeader();
            UpdateButtonStates();
            SequenceChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Clear()
        {
            _notes.Clear();

            foreach (var chip in _chips)
            {
                chip.RemoveClicked -= OnChipRemoveClicked;
                chip.Dispose();
            }
            _chips.Clear();

            _bottomSpacer?.Dispose();
            _bottomSpacer = null;

            UpdateHeader();
            UpdateButtonStates();
            SequenceChanged?.Invoke(this, EventArgs.Empty);
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
            _headerLabel.Text = $"Notes: {_notes.Count}";
        }

        private void UpdateButtonStates()
        {
            var hasNotes = _notes.Count > 0;
            _undoButton.Enabled = hasNotes;
            _clearButton.Enabled = hasNotes;
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
                chip.RemoveClicked -= OnChipRemoveClicked;
                chip.Dispose();
            }
            _chips.Clear();

            _chipsContainer?.Dispose();
            base.DisposeControl();
        }
    }
}
