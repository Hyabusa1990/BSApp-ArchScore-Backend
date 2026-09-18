namespace Fawkes.Api.Core.Model
{
    public class Match
    {
        public int MinExpectedNoOfSets { get; set; }
        public IEnumerable<Scoresheet> Scoresheets { get; set; }
    }
}
