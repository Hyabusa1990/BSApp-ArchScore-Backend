namespace Fawkes.Api.Core.Model
{
    public class Scoresheet(int noOfSets, int noOfShotsPerSet, int noOfShootOffShotsForShootOff)
    {
        private Set[] sets = Enumerable.Range(1, noOfSets).Select(_ => new Set(noOfShotsPerSet)).ToArray();
        private Set shootOff = new Set(noOfShootOffShotsForShootOff);


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

        public class Set(int noOfShotsExpected)
        {
            public string Shots { get; set; } = string.Empty;
            public int? Score { get; set; }
            public int NoOfShots { get; set; } = 0;
            public bool IsConfirmed { get; set; }
            public bool IsComplete => Shots.Length >= noOfShotsExpected;
        }
    }
}
