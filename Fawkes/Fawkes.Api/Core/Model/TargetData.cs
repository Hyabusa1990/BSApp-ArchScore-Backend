using Microsoft.Extensions.Primitives;

namespace Fawkes.Api.Core.Model
{
    public class TargetData
    {
        public int TargetNo { get; set; }
        public int RoundNo { get; set; }
        public int CurrentSetNo { get; set; }
        public string TeamName { get; set; } = string.Empty;

        public string Shots { get; set; } = string.Empty;
        public int? ConfirmedSet01Score { get; set; }
        public int? ConfirmedSet02Score { get; set; }
        public int? ConfirmedSet03Score { get; set; }
        public int? ConfirmedSet04Score { get; set; }
        public int? ConfirmedSet05Score { get; set; }

    }




}
