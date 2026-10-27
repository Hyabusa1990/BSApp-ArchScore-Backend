namespace Fawkes.Core.Model
{
    public class Match
    {
        public int MinExpectedNoOfSets { get; set; }
        public IEnumerable<Scoresheet> Scoresheets { get; set; }

        /// <summary>
        /// The round the match belongs to (all scoresheets of a match share it).
        /// </summary>
        public int RoundNo => Scoresheets.Select(s => s.RoundNo).FirstOrDefault();

        /// <summary>
        /// A match is complete once every team has received its match points.
        /// </summary>
        public bool IsComplete => Scoresheets.All(s => s.MatchPoints != null);
    }
}
