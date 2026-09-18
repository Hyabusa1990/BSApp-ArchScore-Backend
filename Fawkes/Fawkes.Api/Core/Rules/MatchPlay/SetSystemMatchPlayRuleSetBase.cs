using Fawkes.Api.Core.Model;

namespace Fawkes.Api.Core.Rules.MatchPlay
{
    public class SetSystemMatchPlayRuleSetBase(int maxSets, bool shootOffRequiredOnDraw) : IMatchPlayRuleSet
    {



        public Match EvaluateMatch(params Scoresheet[] scoresheets)
        {

            var setNo = 1;
            foreach (var scoresheet in scoresheets)
            {
                scoresheet.SetPoints = 0;
                scoresheet.MatchPoints = null;
            }
            while (setNo <= maxSets)
            {
                if (scoresheets.Any(s => !s[setNo].IsConfirmed))
                    break;

                var maxScore = scoresheets.Max(s => s[setNo].Score ?? 0);
                var setWinners = scoresheets.Where(s => s[setNo].Score == maxScore).ToList();
                var setPointsForWinners = setWinners.Count == 1 ? 2 : 1;
                foreach (var winner in setWinners)
                {
                    winner.SetPoints += setPointsForWinners;
                }

                var matchWinner = scoresheets.FirstOrDefault(s => s.SetPoints > maxSets);

                if (matchWinner != null)
                {
                    matchWinner.MatchPoints = 2;
                    foreach (var loser in scoresheets.Where(s => s != matchWinner))
                    {
                        loser.MatchPoints = 0;
                    }
                    break;
                }

                setNo++;


            }

            if (scoresheets.All(s => s.MatchPoints == null) && setNo > maxSets)
            {
                if (shootOffRequiredOnDraw)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    foreach (var scoresheet in scoresheets)
                    {
                        scoresheet.MatchPoints = 1;
                    }
                }
            }


            var matchComplete = scoresheets.All(s => s.MatchPoints != null);

            return new Match()
            {
                MinExpectedNoOfSets = matchComplete? Math.Min(maxSets, setNo) : Math.Min(maxSets, scoresheets.Min(s => setNo - 1 + Math.Max(0,(maxSets + 2 - s.SetPoints) / 2))),
                Scoresheets = scoresheets
            };


        }
    }
}
