using Fawkes.Api.Core.Rules.League;
using Fawkes.Api.Core.Rules.MatchPlay;
using Fawkes.Api.Core.Rules.Scoring;

namespace Fawkes.Api.Core.Rules
{
    [TestFixture]
    public class RuleSetFactoryTests
    {
        [Test]
        public void FactoryReturnsDefaultRuleSets()
        {
            var factory = new RuleSetFactory();

            Assert.That(factory.GetScoringRuleSet(RuleSetKeys.Default), Is.TypeOf<ScoringRuleSetBase>());
            Assert.That(factory.GetLeagueRuleSet(RuleSetKeys.Default), Is.TypeOf<StandardLeagueRuleSet>());

            var matchPlayRules = factory.GetMatchPlayRuleSet(RuleSetKeys.Default);
            Assert.That(matchPlayRules, Is.TypeOf<SetSystemMatchPlayRuleSetBase>());
            Assert.That(matchPlayRules.MaxEndNo, Is.EqualTo(5));
        }
    }
}
