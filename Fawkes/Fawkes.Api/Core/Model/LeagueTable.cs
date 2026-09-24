using static Fawkes.Api.Controllers.DisplaysController;

namespace Fawkes.Api.Core.Model
{
    public class LeagueTable
    {
        public IEnumerable<LeagueTablePosition> Positions { get; set; }
    }

    public class LeagueTablePosition
    {
        public int Rank { get; set; }
        public int RankDifference { get; set; } 
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int MatchPointsWon { get; set; }
        public int MatchPointsLost { get; set; }
        public int SetPointsWon { get; set; }
        public int SetPointsLost { get; set; }
        public int SetPointsDifference => SetPointsWon - SetPointsLost;
        public int TotalScore { get; set; }
    }
}
