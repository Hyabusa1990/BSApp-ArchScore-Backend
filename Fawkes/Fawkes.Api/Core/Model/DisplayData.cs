namespace Fawkes.Api.Core.Model
{
    public abstract class DisplayData
    {
        public DisplayTheme Theme { get; set; }
    }

    public class NoDisplayData : DisplayData
    {
    }

    public class MatchDisplayData : DisplayData
    {



        public IEnumerable<MatchTargetDisplayData> Targets { get; set; }

        public class MatchTargetDisplayData
        {
            public int TargetNo { get; set; }
            public int CurrentSetNo { get; set; }
            public string TeamName { get; set; } = string.Empty;
            public string Shots { get; set; } = string.Empty;
            public int? CurrentSetScore { get; set; }
            public int?[] SetScores { get; set; } = Array.Empty<int?>();
            public int SetPoints { get; set; }
            public bool IsConfirmed { get; set; }
        }

    }

}
