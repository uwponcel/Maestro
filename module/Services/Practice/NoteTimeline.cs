using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Maestro.Services.Practice
{
    public struct TimelineNote
    {
        public int StartMs;
        public int DurationMs;
        public int OctaveAtPlay;
        public bool IsSharp;

        /// <summary>1-8: C D E F G A B C^. Sharps share their natural's lane.</summary>
        public int Lane;
    }

    public struct OctaveShiftPoint
    {
        public int AtMs;
        public int Delta;
    }

    public class NoteTimeline
    {
        private static readonly Regex NotePattern = new Regex(
            @"([A-GR])(\^|#)?([+-])?:(\d+)",
            RegexOptions.Compiled);

        public IReadOnlyList<TimelineNote> Notes { get; }
        public IReadOnlyList<OctaveShiftPoint> OctaveShiftPoints { get; }
        public int TotalDurationMs { get; }

        private NoteTimeline(List<TimelineNote> notes, List<OctaveShiftPoint> shifts, int total)
        {
            Notes = notes;
            OctaveShiftPoints = shifts;
            TotalDurationMs = total;
        }

        public static NoteTimeline Build(IReadOnlyList<string> noteLines)
        {
            var notes = new List<TimelineNote>();
            var shifts = new List<OctaveShiftPoint>();
            int currentOctave = 0;
            int currentMs = 0;

            foreach (var line in noteLines)
            {
                var matches = NotePattern.Matches(line);
                if (matches.Count == 0) continue;

                int maxDuration = 0;

                foreach (Match m in matches)
                {
                    var pitch = m.Groups[1].Value;
                    var modifier = m.Groups[2].Success ? m.Groups[2].Value : null;
                    var octaveMarker = m.Groups[3].Success ? m.Groups[3].Value : null;
                    var duration = int.Parse(m.Groups[4].Value);
                    if (duration > maxDuration) maxDuration = duration;

                    if (pitch == "R") continue;

                    int targetOctave = octaveMarker == "+" ? 1 : octaveMarker == "-" ? -1 : 0;
                    if (targetOctave != currentOctave)
                    {
                        int delta = targetOctave > currentOctave ? 1 : -1;
                        int steps = Math.Abs(targetOctave - currentOctave);
                        for (int s = 0; s < steps; s++)
                        {
                            shifts.Add(new OctaveShiftPoint { AtMs = currentMs, Delta = delta });
                        }
                        currentOctave = targetOctave;
                    }

                    bool isSharp = modifier == "#";
                    bool isHighC = modifier == "^" && pitch == "C";

                    int lane = isHighC ? 8 : PitchToLane(pitch[0]);
                    if (lane == 0) continue;
                    if (isSharp && !SharpExistsOnLane(lane)) continue; // no E# or B# in GW2

                    notes.Add(new TimelineNote
                    {
                        StartMs = currentMs,
                        DurationMs = duration,
                        OctaveAtPlay = currentOctave,
                        IsSharp = isSharp,
                        Lane = lane,
                    });
                }

                currentMs += maxDuration;
            }

            return new NoteTimeline(notes, shifts, currentMs);
        }

        /// <summary>Lane letter for display: C D E F G A B, lane 8 = high C.</summary>
        public static char LaneLetter(int lane)
        {
            switch (lane)
            {
                case 1: case 8: return 'C';
                case 2: return 'D';
                case 3: return 'E';
                case 4: return 'F';
                case 5: return 'G';
                case 6: return 'A';
                case 7: return 'B';
                default: return '?';
            }
        }

        private static int PitchToLane(char pitch)
        {
            switch (pitch)
            {
                case 'C': return 1;
                case 'D': return 2;
                case 'E': return 3;
                case 'F': return 4;
                case 'G': return 5;
                case 'A': return 6;
                case 'B': return 7;
                default: return 0;
            }
        }

        // GW2 instruments expose C#, D#, F#, G#, A# only.
        private static bool SharpExistsOnLane(int lane) =>
            lane == 1 || lane == 2 || lane == 4 || lane == 5 || lane == 6;

        public List<int> GetNoteIndicesInWindow(int fromMs, int toMs)
        {
            var result = new List<int>();
            int lo = LowerBound(fromMs);
            for (int i = lo; i < Notes.Count && Notes[i].StartMs <= toMs; i++)
            {
                result.Add(i);
            }
            return result;
        }

        private int LowerBound(int fromMs)
        {
            int lo = 0, hi = Notes.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (Notes[mid].StartMs < fromMs) lo = mid + 1;
                else hi = mid;
            }
            return lo;
        }
    }
}
