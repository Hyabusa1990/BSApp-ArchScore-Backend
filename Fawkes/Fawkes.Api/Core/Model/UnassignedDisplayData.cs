namespace Fawkes.Api.Core.Model
{
    public class UnassignedDisplayData : DisplayData
    {
        public string DeviceCode { get; set; }
    }

    public class TableDisplayData : DisplayData
    {
        public IEnumerable<LeagueTablePosition> Positions { get; set; }

    }

}
