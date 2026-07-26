using System.Collections.Generic;
using Maestro.Services.Practice;
using Xunit;

namespace Maestro.Tests.Practice
{
    public class NoteTimelineTests
    {
        [Fact]
        public void Build_SingleNaturalNote_ProducesOneTimelineNote()
        {
            var notes = new List<string> { "C:200" };

            var timeline = NoteTimeline.Build(notes);

            Assert.Single(timeline.Notes);
            Assert.Equal(0, timeline.Notes[0].StartMs);
            Assert.Equal(200, timeline.Notes[0].DurationMs);
            Assert.Equal(1, timeline.Notes[0].Lane);
            Assert.Equal(0, timeline.Notes[0].OctaveAtPlay);
            Assert.False(timeline.Notes[0].IsSharp);
            Assert.Equal(200, timeline.TotalDurationMs);
        }

        [Fact]
        public void Build_Chord_TwoNotesShareStartTime()
        {
            var notes = new List<string> { "C:200 E:200" };
            var timeline = NoteTimeline.Build(notes);
            Assert.Equal(2, timeline.Notes.Count);
            Assert.Equal(0, timeline.Notes[0].StartMs);
            Assert.Equal(0, timeline.Notes[1].StartMs);
            Assert.Equal(1, timeline.Notes[0].Lane);
            Assert.Equal(3, timeline.Notes[1].Lane);
            Assert.Equal(200, timeline.TotalDurationMs);
        }

        [Fact]
        public void Build_OctaveShift_EmitsShiftPointAndTracksState()
        {
            var notes = new List<string> { "C:100", "C+:100" };
            var timeline = NoteTimeline.Build(notes);
            Assert.Equal(2, timeline.Notes.Count);
            Assert.Equal(0, timeline.Notes[0].OctaveAtPlay);
            Assert.Equal(1, timeline.Notes[1].OctaveAtPlay);
            Assert.Single(timeline.OctaveShiftPoints);
            Assert.Equal(100, timeline.OctaveShiftPoints[0].AtMs);
            Assert.Equal(1, timeline.OctaveShiftPoints[0].Delta);
        }

        [Fact]
        public void Build_Rest_AdvancesTimeWithoutNote()
        {
            var notes = new List<string> { "C:100", "R:300", "D:100" };
            var timeline = NoteTimeline.Build(notes);
            Assert.Equal(2, timeline.Notes.Count);
            Assert.Equal(0, timeline.Notes[0].StartMs);
            Assert.Equal(400, timeline.Notes[1].StartMs);
            Assert.Equal(500, timeline.TotalDurationMs);
        }

        [Fact]
        public void Build_HighC_MapsToLane8()
        {
            var notes = new List<string> { "C^:100" };
            var timeline = NoteTimeline.Build(notes);
            Assert.Equal(8, timeline.Notes[0].Lane);
            Assert.False(timeline.Notes[0].IsSharp);
        }

        [Fact]
        public void Build_Sharp_MapsToNaturalLaneWithIsSharp()
        {
            var notes = new List<string> { "C#:100" };
            var timeline = NoteTimeline.Build(notes);
            Assert.Equal(1, timeline.Notes[0].Lane);
            Assert.True(timeline.Notes[0].IsSharp);
        }

        [Fact]
        public void GetNoteIndicesInWindow_ExactBoundaries_Inclusive()
        {
            var notes = new List<string> { "C:100", "D:100", "E:100", "F:100" };
            var timeline = NoteTimeline.Build(notes);
            var result = timeline.GetNoteIndicesInWindow(100, 200);
            Assert.Equal(new[] { 1, 2 }, result);
        }

        [Fact]
        public void GetNoteIndicesInWindow_EmptyRange_ReturnsEmpty()
        {
            var notes = new List<string> { "C:100", "D:100" };
            var timeline = NoteTimeline.Build(notes);
            Assert.Empty(timeline.GetNoteIndicesInWindow(500, 1000));
        }
    }
}
