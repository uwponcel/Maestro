using System.Collections.Generic;
using Maestro.Models;
using Maestro.Services.Practice;
using Xunit;

namespace Maestro.Tests.Practice
{
    public class FakeKeySender : IKeySender
    {
        public int UpCount;
        public int DownCount;
        public void SendOctaveUp() => UpCount++;
        public void SendOctaveDown() => DownCount++;
    }

    public class PracticeSessionTests
    {
        private static PracticeSession NewSession(
            IReadOnlyList<string> notes,
            bool autoOctave = true,
            IKeySender sender = null)
        {
            var timeline = NoteTimeline.Build(notes);
            return new PracticeSession(timeline, sender ?? new FakeKeySender(), autoOctave, countdownMs: 0);
        }

        [Fact]
        public void PerfectKeyPress_AtStart_RecordsPerfect()
        {
            var session = NewSession(new[] { "C:200" });
            Judgement? last = null;
            session.OnJudgement += j => last = j;

            session.OnPlayerNotePressed(1, false, 10);

            Assert.Equal(JudgementVerdict.Perfect, last.Value.Verdict);
            Assert.Equal(1, session.PerfectCount);
            Assert.Equal(1, session.Combo);
        }

        [Fact]
        public void Miss_AfterGoodWindow_BreaksCombo()
        {
            var session = NewSession(new[] { "C:200", "D:200" });
            session.OnPlayerNotePressed(1, false, 0);
            Assert.Equal(1, session.Combo);

            session.Tick(400);

            Assert.Equal(1, session.MissCount);
            Assert.Equal(0, session.Combo);
        }

        [Fact]
        public void WrongKey_BreaksCombo_RecordsWrong()
        {
            var session = NewSession(new[] { "C:200" });
            session.OnPlayerNotePressed(1, false, 0);
            session.OnPlayerNotePressed(2, false, 10);
            Assert.Equal(0, session.Combo);
            Assert.Equal(1, session.WrongCount);
        }

        [Fact]
        public void AutoOctave_InjectsShiftAtShiftPoint()
        {
            var sender = new FakeKeySender();
            var session = NewSession(new[] { "C:200", "C+:200" }, autoOctave: true, sender: sender);

            session.Tick(200);

            Assert.Equal(1, sender.UpCount);
            Assert.Equal(0, sender.DownCount);
        }

        [Fact]
        public void AutoOctaveDisabled_DoesNotInject()
        {
            var sender = new FakeKeySender();
            var session = NewSession(new[] { "C:200", "C+:200" }, autoOctave: false, sender: sender);

            session.Tick(200);

            Assert.Equal(0, sender.UpCount);
        }

        [Fact]
        public void LoopSeek_ResetsHitStateInRange_PreservesScoreTotals()
        {
            var session = NewSession(new[] { "C:200", "D:200", "E:200" });
            session.OnPlayerNotePressed(1, false, 0);
            session.OnPlayerNotePressed(2, false, 200);
            Assert.Equal(2, session.PerfectCount);

            session.LoopSeek(0, 400);
            Assert.Equal(2, session.PerfectCount);
            Assert.Equal(0, session.Clock.CurrentMs);

            session.OnPlayerNotePressed(1, false, 0);
            Assert.Equal(3, session.PerfectCount);
        }

        [Fact]
        public void Countdown_BlocksGrading()
        {
            var timeline = NoteTimeline.Build(new[] { "C:200" });
            var session = new PracticeSession(timeline, new FakeKeySender(), autoOctave: true, countdownMs: 3000);

            session.OnPlayerNotePressed(1, false, -500);

            Assert.Equal(0, session.PerfectCount);
        }

        [Fact]
        public void AutoOctave_ClampsAtInstrumentMaxOctave()
        {
            // Instrument with no octave above 0 (e.g. Flute's top): the "+" shift
            // must never send an octave-up key.
            var sender = new FakeKeySender();
            var timeline = NoteTimeline.Build(new[] { "C:200", "C+:200" });
            var session = new PracticeSession(timeline, sender, autoOctave: true, countdownMs: 0, minOctave: -1, maxOctave: 0);

            session.Tick(200);

            Assert.Equal(0, sender.UpCount);
        }

        [Fact]
        public void LoopSeek_ResyncsOctaveToLoopStart()
        {
            // Play into the high-octave section, then loop back to the start: the
            // session must send a down to return to octave 0, and going up again on
            // the next pass must not overshoot.
            var sender = new FakeKeySender();
            var session = NewSession(new[] { "C:200", "C+:200" }, sender: sender);

            session.Tick(200);
            Assert.Equal(1, sender.UpCount);

            session.LoopSeek(0, 400);
            Assert.Equal(1, sender.DownCount);

            session.Tick(200);
            Assert.Equal(2, sender.UpCount);
            Assert.Equal(1, sender.DownCount);
        }

        [Fact]
        public void SeekTo_ForwardJump_SyncsOctaveWithoutMissFlood()
        {
            var sender = new FakeKeySender();
            var session = NewSession(new[] { "C:200", "C+:200", "C+:200" }, sender: sender);
            int misses = 0;
            session.OnJudgement += j => { if (j.Verdict == JudgementVerdict.Miss) misses++; };

            session.SeekTo(400);
            session.Tick(10);

            Assert.Equal(1, sender.UpCount);   // synced to the +1 octave at 400ms
            Assert.Equal(0, misses);           // skipped notes are not judged
        }
    }
}
