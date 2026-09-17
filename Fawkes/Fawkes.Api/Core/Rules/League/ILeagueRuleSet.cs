using Fawkes.Api.Core.Model;

namespace Fawkes.Api.Core.Rules.League
{
    public interface ILeagueRuleSet
    {
        LeagueTable CalculateLeagueTable(LeagueTable initialTable, params Match[] matches);
    }
}
