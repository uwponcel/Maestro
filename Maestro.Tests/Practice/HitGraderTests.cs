using System.Collections.Generic;
using Maestro.Models;
using Maestro.Services.Practice;
using Xunit;

namespace Maestro.Tests.Practice
{
    public class HitGraderTests
    {
        private const int PerfectMs = 50;
        private const int GoodMs = 120;

        private static NoteTimeline Tl(params string[] lines) => NoteTimeline.Build(lines);

        [Fact]
        public void Grade_WithinPerfectWindow_ReturnsPerfect()
        {
            var tl = Tl("C:200");
            var wasHit = new bool[tl.Notes.Count];

            var result = HitGrader.Grade(1, false, 30, tl, wasHit, PerfectMs, GoodMs);

            Assert.NotNull(result);
            Assert.Equal(JudgementVerdict.Perfect, result.Value.Verdict);
            Assert.Equal(30, result.Value.DeltaMs);
        }

        [Fact]
        public void Grade_AtExactPerfectBoundary_StillPerfect()
        {
            var tl = Tl("C:200");
            var wasHit = new bool[1];
            var result = HitGrader.Grade(1, false, 50, tl, wasHit, PerfectMs, GoodMs);
            Assert.Equal(JudgementVerdict.Perfect, result.Value.Verdict);
        }

        [Fact]
        public void Grade_OneMsPastPerfect_IsGood()
        {
            var tl = Tl("C:200");
            var wasHit = new bool[1];
            var result = HitGrader.Grade(1, false, 51, tl, wasHit, PerfectMs, GoodMs);
            Assert.Equal(JudgementVerdict.Good, result.Value.Verdict);
        }

        [Fact]
        public void Grade_BeyondGoodWindow_ReturnsNull()
        {
            var tl = Tl("C:200");
            var wasHit = new bool[1];
            var result = HitGrader.Grade(1, false, 121, tl, wasHit, PerfectMs, GoodMs);
            Assert.Null(result);
        }

        [Fact]
        public void Grade_WrongKey_ReturnsNull()
        {
            var tl = Tl("C:200");
            var wasHit = new bool[1];
            var result = HitGrader.Grade(2, false, 0, tl, wasHit, PerfectMs, GoodMs);
            Assert.Null(result);
        }

        [Fact]
        public void Grade_AlreadyHitNote_NotReturned()
        {
            var tl = Tl("C:200");
            var wasHit = new bool[] { true };
            var result = HitGrader.Grade(1, false, 10, tl, wasHit, PerfectMs, GoodMs);
            Assert.Null(result);
        }

        [Fact]
        public void Grade_TwoCandidates_NearerNoteWins()
        {
            var tl = Tl("C:50", "C:50");
            var wasHit = new bool[2];
            var result = HitGrader.Grade(1, false, 40, tl, wasHit, PerfectMs, GoodMs);
            Assert.Equal(1, result.Value.TimelineIndex);
        }
    }
}
