using System;
using System.Linq;
using Blish_HUD;
using Blish_HUD.Controls;
using Maestro.Models;
using Microsoft.Xna.Framework;

namespace Maestro.UI.MaestroCreator
{
    public class DurationSelector : Panel
    {
        public static class Layout
        {
            public const int Height = 30;
            public const int BpmLabelWidth = 40;
            public const int BpmInputWidth = 50;
            public const int NoteButtonWidth = 40;
            public const int NoteButtonHeight = 26;
            public const int Spacing = 5;
            public const int NoteButtonsLeftMargin = 15;
            public const int NoteButtonGap = 2;

            // BPM range based on music theory:
            // Min 20: Larghissimo (extremely slow, rare but valid)
            // Max 300: Prestissimo and fast electronic/metal music
            public const int MinBpm = 20;
            public const int MaxBpm = 300;
        }

        public event EventHandler DurationChanged;

        private int _bpm = 120;
        private string _lastValidBpmText = "120";

        public int Bpm
        {
            get => _bpm;
            set
            {
                _bpm = ClampBpm(value);
                UpdateBpmDisplay();
                DurationChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private static int ClampBpm(int value)
        {
            return value < Layout.MinBpm ? Layout.MinBpm : (value > Layout.MaxBpm ? Layout.MaxBpm : value);
        }

        private NoteType _selectedNoteType = NoteType.Quarter;
        public NoteType SelectedNoteType
        {
            get => _selectedNoteType;
            set
            {
                _selectedNoteType = value;
                UpdateNoteTypeSelection();
                DurationChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public int CurrentDurationMs => _selectedNoteType.GetDurationMs(_bpm);

        private readonly Label _bpmLabel;
        private readonly TextBox _bpmInput;
        private readonly Panel[] _noteButtons;
        private readonly Label[] _noteLabels;
        private readonly NoteType[] _noteTypes = { NoteType.Whole, NoteType.Half, NoteType.Quarter, NoteType.Eighth, NoteType.Sixteenth };

        public DurationSelector(int width)
        {
            Size = new Point(width, Layout.Height);
            BackgroundColor = Color.Transparent;

            _noteButtons = new Panel[_noteTypes.Length];
            _noteLabels = new Label[_noteTypes.Length];

            _bpmLabel = new Label
            {
                Parent = this,
                Text = "BPM:",
                Location = new Point(0, 0),
                Size = new Point(Layout.BpmLabelWidth, Layout.NoteButtonHeight),
                Font = GameService.Content.DefaultFont12,
                TextColor = MaestroTheme.CreamWhite,
                VerticalAlignment = VerticalAlignment.Middle
            };

            _bpmInput = new TextBox
            {
                Parent = this,
                Text = _bpm.ToString(),
                Location = new Point(Layout.BpmLabelWidth, 0),
                Size = new Point(Layout.BpmInputWidth, Layout.NoteButtonHeight),
                Font = GameService.Content.DefaultFont12,
                BasicTooltipText = $"Tempo in beats per minute ({Layout.MinBpm}-{Layout.MaxBpm})"
            };
            _bpmInput.TextChanged += OnBpmTextChanged;
            _bpmInput.InputFocusChanged += OnBpmInputFocusChanged;

            // Note type buttons (Panel-based for selected state)
            var noteX = Layout.BpmLabelWidth + Layout.BpmInputWidth + Layout.NoteButtonsLeftMargin;
            for (int i = 0; i < _noteTypes.Length; i++)
            {
                var noteType = _noteTypes[i];
                var isSelected = noteType == _selectedNoteType;
                var tooltipText = $"{noteType.GetDisplayName()} note ({noteType.GetDurationMs(_bpm)}ms @ {_bpm} BPM)";

                var button = new Panel
                {
                    Parent = this,
                    Location = new Point(noteX + i * (Layout.NoteButtonWidth + Layout.NoteButtonGap), 0),
                    Size = new Point(Layout.NoteButtonWidth, Layout.NoteButtonHeight),
                    BackgroundColor = isSelected ? MaestroTheme.AmberGold : MaestroTheme.ButtonBackground,
                    BasicTooltipText = tooltipText
                };

                var label = new Label
                {
                    Parent = button,
                    Text = GetNoteButtonText(noteType),
                    Location = new Point(0, 0),
                    Size = new Point(Layout.NoteButtonWidth, Layout.NoteButtonHeight),
                    Font = GameService.Content.DefaultFont12,
                    TextColor = isSelected ? MaestroTheme.DarkCharcoal : MaestroTheme.CreamWhite,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Middle,
                    BasicTooltipText = tooltipText
                };

                var capturedIndex = i;
                var capturedNoteType = noteType;

                button.MouseEntered += (s, e) =>
                {
                    var sel = _noteTypes[capturedIndex] == _selectedNoteType;
                    button.BackgroundColor = sel ? MaestroTheme.DeepAmber : MaestroTheme.ButtonBackgroundHover;
                };
                button.MouseLeft += (s, e) =>
                {
                    var sel = _noteTypes[capturedIndex] == _selectedNoteType;
                    button.BackgroundColor = sel ? MaestroTheme.AmberGold : MaestroTheme.ButtonBackground;
                };
                button.LeftMouseButtonReleased += (s, e) => SelectedNoteType = capturedNoteType;

                _noteButtons[i] = button;
                _noteLabels[i] = label;
            }

            UpdateNoteTypeSelection();
        }

        private string GetNoteButtonText(NoteType noteType)
        {
            // Use simpler text since Unicode symbols may not render well
            switch (noteType)
            {
                case NoteType.Whole:
                    return "1";
                case NoteType.Half:
                    return "1/2";
                case NoteType.Quarter:
                    return "1/4";
                case NoteType.Eighth:
                    return "1/8";
                case NoteType.Sixteenth:
                    return "1/16";
                default:
                    return "1/4";
            }
        }

        private void OnBpmTextChanged(object sender, EventArgs e)
        {
            var text = _bpmInput.Text;
            var numbersOnly = new string(text.Where(char.IsDigit).ToArray());

            if (numbersOnly != text)
            {
                _bpmInput.Text = numbersOnly;
                return;
            }

            if (string.IsNullOrEmpty(numbersOnly))
                return;

            if (int.TryParse(numbersOnly, out var newBpm))
            {
                _bpm = ClampBpm(newBpm);
                _lastValidBpmText = _bpm.ToString();
                UpdateTooltips();
                DurationChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnBpmInputFocusChanged(object sender, ValueEventArgs<bool> e)
        {
            // When focus is lost (e.Value == false), clamp and update display
            if (!e.Value)
            {
                if (string.IsNullOrEmpty(_bpmInput.Text) || !int.TryParse(_bpmInput.Text, out var parsedBpm))
                {
                    // Empty or invalid - restore last valid value
                    _bpmInput.Text = _lastValidBpmText;
                }
                else
                {
                    // Clamp to valid range and update display
                    _bpm = ClampBpm(parsedBpm);
                    _lastValidBpmText = _bpm.ToString();
                    _bpmInput.Text = _lastValidBpmText;
                    UpdateTooltips();
                    DurationChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void UpdateBpmDisplay()
        {
            if (_bpmInput != null && _bpmInput.Text != _bpm.ToString())
            {
                _bpmInput.Text = _bpm.ToString();
            }
            UpdateTooltips();
        }

        private void UpdateTooltips()
        {
            for (int i = 0; i < _noteTypes.Length; i++)
            {
                var noteType = _noteTypes[i];
                var tooltipText = $"{noteType.GetDisplayName()} note ({noteType.GetDurationMs(_bpm)}ms @ {_bpm} BPM)";
                _noteButtons[i].BasicTooltipText = tooltipText;
                _noteLabels[i].BasicTooltipText = tooltipText;
            }
        }

        private void UpdateNoteTypeSelection()
        {
            for (int i = 0; i < _noteTypes.Length; i++)
            {
                var isSelected = _noteTypes[i] == _selectedNoteType;
                _noteButtons[i].BackgroundColor = isSelected ? MaestroTheme.AmberGold : MaestroTheme.ButtonBackground;
                _noteLabels[i].TextColor = isSelected ? MaestroTheme.DarkCharcoal : MaestroTheme.CreamWhite;
            }
        }

        protected override void DisposeControl()
        {
            if (_bpmInput != null)
            {
                _bpmInput.TextChanged -= OnBpmTextChanged;
                _bpmInput.InputFocusChanged -= OnBpmInputFocusChanged;
            }

            _bpmLabel?.Dispose();
            _bpmInput?.Dispose();

            foreach (var label in _noteLabels)
                label?.Dispose();

            foreach (var button in _noteButtons)
                button?.Dispose();

            base.DisposeControl();
        }
    }
}
