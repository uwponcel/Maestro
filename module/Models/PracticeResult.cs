namespace Maestro.Models
{
    public class PracticeResult
    {
        public int PerfectCount { get; set; }
        public int GoodCount { get; set; }
        public int MissCount { get; set; }
        public int WrongCount { get; set; }
        public int MaxCombo { get; set; }

        public int TotalNotes => PerfectCount + GoodCount + MissCount;

        public double AccuracyPercent => TotalNotes == 0
            ? 0
            : ((PerfectCount * 100.0) + (GoodCount * 60.0)) / TotalNotes;
    }
}
