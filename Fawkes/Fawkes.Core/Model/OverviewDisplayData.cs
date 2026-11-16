namespace Fawkes.Core.Model
{
    /// <summary>
    /// League table together with every match of the current round.
    /// </summary>
    public class OverviewDisplayData : DisplayData
    {
        public IEnumerable<LeagueTablePosition> Positions { get; set; } = [];

        public IEnumerable<OverviewMatch> Matches { get; set; } = [];

        public class OverviewMatch
        {
            public int MatchNo { get; set; }
            public int MinNoOfSets { get; set; }
            public IEnumerable<MatchDisplayData.MatchTargetDisplayData> Targets { get; set; } = [];
        }
    }

}
