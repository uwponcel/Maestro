using System;
using Maestro.Models;

namespace Maestro.Services.Practice
{
    public static class HitGrader
    {
        /// <summary>
        /// Grades a player press identified by (lane, isSharp) against unhit timeline notes
        /// near <paramref name="nowMs"/>. Sharps are strict: pressing the natural key does
        /// not hit a sharp note and vice versa.
        /// </summary>
        public static Judgement? Grade(
            int lane,
            bool isSharp,
            int nowMs,
            NoteTimeline timeline,
            bool[] wasHit,
            int perfectMs,
            int goodMs)
        {
            int bestIndex = -1;
            int bestAbsDelta = int.MaxValue;

            var candidates = timeline.GetNoteIndicesInWindow(nowMs - goodMs, nowMs + goodMs);
            foreach (var idx in candidates)
            {
                if (wasHit[idx]) continue;
                var note = timeline.Notes[idx];
                if (note.Lane != lane || note.IsSharp != isSharp) continue;
                int delta = nowMs - note.StartMs;
                int abs = Math.Abs(delta);
                if (abs > goodMs) continue;
                if (abs < bestAbsDelta || (abs == bestAbsDelta && note.StartMs < timeline.Notes[bestIndex].StartMs))
                {
                    bestAbsDelta = abs;
                    bestIndex = idx;
                }
            }

            if (bestIndex < 0) return null;

            var target = timeline.Notes[bestIndex];
            int deltaMs = nowMs - target.StartMs;
            var verdict = Math.Abs(deltaMs) <= perfectMs
                ? JudgementVerdict.Perfect
                : JudgementVerdict.Good;

            return new Judgement
            {
                Verdict = verdict,
                DeltaMs = deltaMs,
                TimelineIndex = bestIndex,
                Lane = target.Lane,
            };
        }
    }
}
