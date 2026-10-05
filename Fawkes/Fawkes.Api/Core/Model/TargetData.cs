using Microsoft.Extensions.Primitives;
using System.Reflection;
using static Fawkes.Api.Store.FawkesDbContext;

namespace Fawkes.Api.Core.Model
{
    public class TargetData
    {
        private static PropertyInfo[] setScoreProps = typeof(TargetAssignment).GetProperties().Where(p => p.Name.StartsWith("ConfirmedSet") && p.Name.EndsWith("Score")).ToArray();


        static TargetData()
        {
            var props = new List<PropertyInfo>();

            var propertyName = nameof(ConfirmedSet01Score).Replace("01", "__");

            var i = 1;
            while (true)
            {
                var prop = typeof(TargetAssignment).GetProperty(propertyName.Replace("__", i.ToString("00")));
                if (prop == null)
                    break;
                props.Add(prop);
                i++;
            }
        }

        public int TargetNo { get; set; }
        public int RoundNo { get; set; }
        public string TeamName { get; set; } = string.Empty;

        public string Shots { get; set; } = string.Empty;
        public int? ConfirmedSet01Score { get; set; }
        public int? ConfirmedSet02Score { get; set; }
        public int? ConfirmedSet03Score { get; set; }
        public int? ConfirmedSet04Score { get; set; }
        public int? ConfirmedSet05Score { get; set; }

        public int CurrentSetNo
        {
            get
            {
                var result = 1;
                while (true)
                {
                    var prop = setScoreProps.FirstOrDefault(_ => _.Name == nameof(ConfirmedSet01Score).Replace("01", $"{result:00}"));
                    if (prop == null || prop.GetValue(this) == null)
                        return result;
                    result++;
                }
            }
        }


    }




}
