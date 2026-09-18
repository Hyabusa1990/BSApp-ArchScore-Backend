namespace Fawkes.Api.Core.Model
{
    public class SpotterTargetData
    {
        public int TargetNo { get; set; }
        public int CurrentSetNo { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string Shots { get; set; } = string.Empty;
        public int? CurrentSetScore { get; set; }
        public bool IsConfirmed { get; set; }
    }

}
