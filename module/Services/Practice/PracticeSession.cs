using System;
using Maestro.Models;

namespace Maestro.Services.Practice
{
    public interface IKeySender
    {
        void SendOctaveUp();
        void SendOctaveDown();
    }

    public class PracticeSession
    {
        public const int PerfectWindowMs = 50;
        public const int GoodWindowMs = 120;

        private readonly IKeySender _sender;
        private readonly bool _autoOctave;
        private readonly int _minOctave;
        private readonly int _maxOctave;

        private readonly bool[] _wasHit;
        private readonly bool[] _firedShifts;

        // Physical instrument octave relative to the post-reset baseline (0). Only
        // meaningful in auto-octave mode, where the session owns octave key sends.
        private int _currentOctave;

        public NoteTimeline Timeline { get; }
        public PracticeClock Clock { get; }

        public int PerfectCount { get; private set; }
        public int GoodCount { get; private set; }
        public int MissCount { get; private set; }
        public int WrongCount { get; private set; }
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        public bool IsCompleted { get; private set; }

        public event Action<Judgement> OnJudgement;
        public event Action<int> OnOctaveShiftFired;
        public event Action OnCountdownComplete;
        public event Action<PracticeResult> OnCompleted;

        public PracticeSession(
            NoteTimeline timeline,
            IKeySender sender,
            bool autoOctave,
            int countdownMs,
            int minOctave = -1,
            int maxOctave = 1)
        {
            Timeline = timeline;
            _sender = sender;
            _autoOctave = autoOctave;
            _minOctave = minOctave;
            _maxOctave = maxOctave;
            _wasHit = new bool[timeline.Notes.Count];
            _firedShifts = new bool[timeline.OctaveShiftPoints.Count];

            Clock = new PracticeClock();
            Clock.Seek(-countdownMs);
        }

        public void Tick(int elapsedMs)
        {
            if (IsCompleted) return;

            int before = Clock.CurrentMs;
            Clock.Advance(elapsedMs);
            int after = Clock.CurrentMs;

            if (before < 0 && after >= 0)
                OnCountdownComplete?.Invoke();

            if (_autoOctave && after >= 0)
            {
                for (int i = 0; i < Timeline.OctaveShiftPoints.Count; i++)
                {
                    if (_firedShifts[i]) continue;
                    var point = Timeline.OctaveShiftPoints[i];
                    if (point.AtMs <= after)
                    {
                        _firedShifts[i] = true;
                        SendOctaveDelta(point.Delta);
                    }
                }
            }

            for (int i = 0; i < Timeline.Notes.Count; i++)
            {
                if (_wasHit[i]) continue;
                var note = Timeline.Notes[i];
                if (note.StartMs + GoodWindowMs < after)
                {
                    _wasHit[i] = true;
                    MissCount++;
                    BreakCombo();
                    OnJudgement?.Invoke(new Judgement
                    {
                        Verdict = JudgementVerdict.Miss,
                        DeltaMs = after - note.StartMs,
                        TimelineIndex = i,
                        Lane = note.Lane,
                    });
                }
            }

            if (!IsCompleted && after >= Timeline.TotalDurationMs + GoodWindowMs)
            {
                IsCompleted = true;
                OnCompleted?.Invoke(BuildResult());
            }
        }

        /// <summary>
        /// Handles a player press already resolved to a highway identity:
        /// <paramref name="lane"/> 1-8 and whether the sharp variant was pressed.
        /// </summary>
        public void OnPlayerNotePressed(int lane, bool isSharp, int nowMs)
        {
            if (IsCompleted) return;
            if (nowMs < 0) return;
            if (lane < 1 || lane > 8) return;

            var result = HitGrader.Grade(lane, isSharp, nowMs, Timeline, _wasHit, PerfectWindowMs, GoodWindowMs);
            if (result == null)
            {
                WrongCount++;
                BreakCombo();
                OnJudgement?.Invoke(new Judgement
                {
                    Verdict = JudgementVerdict.Wrong,
                    DeltaMs = 0,
                    TimelineIndex = -1,
                    Lane = lane,
                });
                return;
            }

            var j = result.Value;
            _wasHit[j.TimelineIndex] = true;
            if (j.Verdict == JudgementVerdict.Perfect) PerfectCount++;
            else if (j.Verdict == JudgementVerdict.Good) GoodCount++;
            Combo++;
            if (Combo > MaxCombo) MaxCombo = Combo;
            OnJudgement?.Invoke(j);
        }

        public void Restart(int countdownMs)
        {
            for (int i = 0; i < _wasHit.Length; i++) _wasHit[i] = false;
            for (int i = 0; i < _firedShifts.Length; i++) _firedShifts[i] = false;
            PerfectCount = GoodCount = MissCount = WrongCount = 0;
            Combo = MaxCombo = 0;
            IsCompleted = false;
            // The owning window physically resets the instrument octave on restart.
            _currentOctave = 0;
            Clock.Seek(-countdownMs);
        }

        /// <summary>
        /// Reset hit state for notes in [fromMs, toMs] so a section loop can pass through them
        /// again, and re-sync the instrument octave to what the timeline expects at the loop
        /// start. Does NOT change score totals; the player's accumulated stats for prior passes
        /// are preserved.
        /// </summary>
        public void LoopSeek(int fromMs, int toMs)
        {
            for (int i = 0; i < Timeline.Notes.Count; i++)
            {
                var n = Timeline.Notes[i];
                if (n.StartMs >= fromMs && n.StartMs <= toMs)
                {
                    _wasHit[i] = false;
                }
            }
            SyncOctaveForSeek(fromMs);
            Clock.Seek(fromMs);
        }

        /// <summary>
        /// Jump the session to <paramref name="ms"/> (loop-bar click). Notes before the target
        /// are marked handled so the jump doesn't flood misses; notes after become gradable
        /// again. The instrument octave is re-synced to the timeline position.
        /// </summary>
        public void SeekTo(int ms)
        {
            ms = Math.Max(0, Math.Min(ms, Timeline.TotalDurationMs));
            for (int i = 0; i < Timeline.Notes.Count; i++)
            {
                _wasHit[i] = Timeline.Notes[i].StartMs + GoodWindowMs < ms;
            }
            SyncOctaveForSeek(ms);
            Clock.Seek(ms);
        }

        /// <summary>
        /// Mark shift points up to <paramref name="ms"/> as applied, re-arm the rest, and in
        /// auto-octave mode inject the key presses needed to move the physical instrument from
        /// its current octave to the octave the timeline expects at <paramref name="ms"/>.
        /// </summary>
        private void SyncOctaveForSeek(int ms)
        {
            for (int i = 0; i < Timeline.OctaveShiftPoints.Count; i++)
            {
                _firedShifts[i] = Timeline.OctaveShiftPoints[i].AtMs <= ms;
            }

            if (!_autoOctave) return;

            int target = TimelineOctaveAt(ms);
            while (_currentOctave != target)
            {
                SendOctaveDelta(target > _currentOctave ? 1 : -1);
            }
        }

        private int TimelineOctaveAt(int ms)
        {
            int octave = 0;
            for (int i = 0; i < Timeline.OctaveShiftPoints.Count; i++)
            {
                if (Timeline.OctaveShiftPoints[i].AtMs <= ms)
                    octave += Timeline.OctaveShiftPoints[i].Delta;
            }
            return Math.Max(_minOctave, Math.Min(_maxOctave, octave));
        }

        /// <summary>
        /// Apply one octave step, clamped to the instrument's range so GW2 can never be
        /// pushed past its top/bottom octave (past the top sits the preset-loops section).
        /// </summary>
        private void SendOctaveDelta(int delta)
        {
            int next = Math.Max(_minOctave, Math.Min(_maxOctave, _currentOctave + delta));
            if (next == _currentOctave) return;

            _currentOctave = next;
            if (delta > 0) _sender.SendOctaveUp();
            else _sender.SendOctaveDown();
            OnOctaveShiftFired?.Invoke(delta);
        }

        public PracticeResult BuildResult() => new PracticeResult
        {
            PerfectCount = PerfectCount,
            GoodCount = GoodCount,
            MissCount = MissCount,
            WrongCount = WrongCount,
            MaxCombo = MaxCombo,
        };

        private void BreakCombo() { Combo = 0; }
    }
}
