using Fawkes.Api.Core.Model;

namespace Fawkes.Api.Core.Rules.League
{
    public class StandardLeagueRuleSet : ILeagueRuleSet
    {
        public LeagueTable CalculateLeagueTable(LeagueTable initialTable, params Match[] matches)
        {

            var evaluateRankDifference = initialTable.Positions.Any(_ => _.MatchPointsWon > 0);

            var allScoresheetsWithOpponents = matches
                .SelectMany(m => m.Scoresheets)
                .Select(s => new
                {
                    TeamScoresheet = s,
                    OpponentScoresheets = matches.First(m => m.Scoresheets.Contains(s)).Scoresheets.Where(_ => _ != s).ToArray()

                })
                .ToList();

            var positions = initialTable.Positions.Select(position =>
            {
                var teamScoresheets = allScoresheetsWithOpponents.Where(s => s.TeamScoresheet.TeamId == position.TeamId).ToList();
                var updatedPosition = new LeagueTablePosition()
                    {
                        TeamId = position.TeamId,
                        TeamName = position.TeamName,
                        SetPointsWon = position.SetPointsWon + teamScoresheets.Sum(s => s.TeamScoresheet.SetPoints),
                        SetPointsLost = position.SetPointsLost + teamScoresheets.Sum(s => s.OpponentScoresheets.Sum(_ => _.SetPoints)),
                        MatchPointsWon = position.MatchPointsWon + teamScoresheets.Sum(s => s.TeamScoresheet.MatchPoints ?? 0),
                        MatchPointsLost = position.MatchPointsLost + teamScoresheets.Sum(s => s.OpponentScoresheets.Sum(_ => _.MatchPoints ?? 0)),
                        TotalScore = position.TotalScore + teamScoresheets.Sum(_ => _.TeamScoresheet.TotalScore)


                    };
                    return updatedPosition;
                })
                .OrderBy(_ => _.MatchPointsWon)
                .ThenBy(_ => _.SetPointsDifference)
                .ThenBy(_ => _.SetPointsWon + _.SetPointsLost > 0 ? _.TotalScore / (_.SetPointsWon + _.SetPointsLost) : 0)
                .ThenBy(_ => _.TeamName)
                .ToList();



            var i = 1;
            var rank = 0;
            var prevPosition = default(LeagueTablePosition);
            foreach(var position in positions)
            {
                if (prevPosition == default || prevPosition.MatchPointsWon != position.MatchPointsWon || prevPosition.SetPointsDifference != position.SetPointsDifference)
                {
                    rank = i;
                }
                i++;

                position.Rank = rank;
                position.RankDifference = 0;

                if (evaluateRankDifference)
                    position.RankDifference = position.Rank - initialTable.Positions.First(p => p.TeamId == position.TeamId).Rank;

                prevPosition = position;
            }




            return new LeagueTable()
            {
                Positions = positions
            };


        }
    }
}
