using Fawkes.Api.Core.Services;

namespace Fawkes.Api.Core.Model
{
    public class Device
    {
        public int Id { get; set; }
        public int? FixtureId { get; set; }
        public required string Code { get; set; }
        public DisplayType DisplayType { get; set; }
        public DisplayTheme DisplayTheme { get; set; }
        public int? MatchNo { get; set; } = null;
    }



}
