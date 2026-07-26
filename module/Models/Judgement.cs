namespace Maestro.Models
{
    public enum JudgementVerdict
    {
        Perfect,
        Good,
        Miss,
        Wrong
    }

    public struct Judgement
    {
        public JudgementVerdict Verdict;
        public int DeltaMs;
        public int TimelineIndex;
        public int Lane;
    }
}
