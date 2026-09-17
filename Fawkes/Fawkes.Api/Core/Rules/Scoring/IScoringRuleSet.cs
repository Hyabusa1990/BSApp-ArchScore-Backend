using Fawkes.Api.Core.Model;

namespace Fawkes.Api.Core.Rules.Scoring
{
    public interface IScoringRuleSet
    {
        int? EvaluateScore(string shots);

        Scoresheet EvaluateScoresheet(TargetData target);
    }

    public class ScoringRuleSetBase(int noOfSets, int noOfShotsPerSet, int noOfShootOffShotsForShootOff, bool xIsEleven = false) : IScoringRuleSet
    {


        public virtual int? EvaluateScore(string shots)
        {
            var evaluatedShots = shots.Select(EvaluateShot).Where(_ => _ != null);

            if (evaluatedShots.Any())
            {
                return evaluatedShots.Sum();
            }
            else
            {
                return null; // No valid shots to evaluate
            }
        }

        public virtual Scoresheet EvaluateScoresheet(TargetData target)
        {
            
            var result = new Scoresheet(noOfSets, noOfShotsPerSet, noOfShootOffShotsForShootOff);
            var shots = (target.Shots ?? string.Empty).ToArray();
            var setScores = new[]
            {
                target.ConfirmedSet01Score,
                target.ConfirmedSet02Score,
                target.ConfirmedSet03Score,
                target.ConfirmedSet04Score,
                target.ConfirmedSet05Score
            };
            for (int setNo = 1; setNo <= noOfSets && shots.Length > 0; setNo++)
            {
                result[setNo].Shots = new string(shots.Take(noOfShotsPerSet).ToArray());
                result[setNo].Score = setScores[setNo - 1] ?? EvaluateScore(result[setNo].Shots);
                result[setNo].IsConfirmed = setScores[setNo - 1].HasValue;
                shots = shots.Skip(noOfShotsPerSet).ToArray();
            }
            return result;
        }

        protected int? EvaluateShot(char shot)
        {
            switch (shot)
            {
                case '-':
                    return xIsEleven ? 11 : 10;
                case '+':
                    return 10;
                case '9':
                    return 9;
                case '8':
                    return 8;
                case '7':
                    return 7;
                case '6':
                    return 6;
                case '5':
                    return 5;
                case '4':
                    return 4;
                case '3':
                    return 3;
                case '2':
                    return 2;
                case '1':
                    return 1;
                case '0':
                case 'M':
                case 'm':
                    return 0; // Miss
                default:
                    return null; // Invalid shot
            }

        }
    }

    public class Scoresheet(int noOfSets, int noOfShotsPerSet, int noOfShootOffShotsForShootOff)
    {
        private Set[] sets = new Set[noOfSets];
        private Set shootOff = new Set();


        public Set this[int setNo]
        {
            get
            {
                if (setNo < 1 || setNo > noOfSets)
                    throw new ArgumentOutOfRangeException(nameof(setNo), $"Set number must be between 1 and {noOfSets}.");
                return sets[setNo - 1];
            }
        }

        public Set ShootOff => shootOff;

        public string AllShots => string.Concat(sets.Select(s => (s.Shots ?? string.Empty).PadRight(noOfShotsPerSet,' ')).Concat(new[] { (shootOff.Shots ?? string.Empty).PadRight(noOfShootOffShotsForShootOff,' ') }));

        public class Set
        {
            public string Shots { get; set; } = string.Empty;
            public int? Score { get; set; }
            public bool IsConfirmed { get; set; }
        }
    }
}
