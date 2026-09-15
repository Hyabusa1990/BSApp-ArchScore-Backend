namespace Fawkes.Api.Core.Model
{
    public class Team
    {
        public int Id { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public int MatchPointsWon { get; set; } = 0;
        public int MatchPointsLost { get; set; } = 0;
        public int SetPointsWon { get; set; } = 0;
        public int SetPointsLost { get; set; } = 0;
    }


}
