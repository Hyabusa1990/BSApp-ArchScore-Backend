using Fawkes.Core.Model;

namespace Fawkes.Core.Rules.League
{
    public interface ILeagueRuleSet
    {
        LeagueTable CalculateLeagueTable(LeagueTable initialTable, params Match[] matches);
    }
}
