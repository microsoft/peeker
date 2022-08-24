using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Peeker.Test
{
    public class MergedRuleSetTests
    {
        [Fact]
        public void Constructor_Empty()
        {
            var mergedRuleSet = new MergedRuleSet(Enumerable.Empty<RuleSet>());

            // Ensure sanity with no files passed
            Assert.NotNull(mergedRuleSet.SpecificDiagnosticOptions);
            Assert.Equal(ReportDiagnostic.Default, mergedRuleSet.GeneralDiagnosticOption);
        }

        [Fact]
        public void Constructor_Null()
        {
            Assert.Throws<ArgumentNullException>(delegate { new MergedRuleSet(null); });
        }

        [Fact]
        public void Constructor_NoParam()
        {
            var mergedRuleSet = new MergedRuleSet();

            Assert.NotNull(mergedRuleSet.SpecificDiagnosticOptions);
            Assert.Equal(ReportDiagnostic.Default, mergedRuleSet.GeneralDiagnosticOption);
        }

        [Fact]
        public void Constructor_MergeOrder()
        {
            var ruleSetA = new RuleSet("FilePath.fake.a", ReportDiagnostic.Default, new Dictionary<string, ReportDiagnostic>()
            {
                {"FA0001", ReportDiagnostic.Default},
                {"FA0002", ReportDiagnostic.Warn}
            }.ToImmutableDictionary(), ImmutableArray.Create<RuleSetInclude>());
            var ruleSetB = new RuleSet("FilePath.fake.b", ReportDiagnostic.Warn, new Dictionary<string, ReportDiagnostic>()
            {
                {"FA0001", ReportDiagnostic.Suppress},
                {"FA0003", ReportDiagnostic.Error}
            }.ToImmutableDictionary(), ImmutableArray.Create<RuleSetInclude>());

            var mergedRuleSet = new MergedRuleSet(new[] { ruleSetA, ruleSetB });

            Assert.Equal(ruleSetB.GeneralDiagnosticOption, mergedRuleSet.GeneralDiagnosticOption);
            Assert.Equal(ReportDiagnostic.Suppress, mergedRuleSet.SpecificDiagnosticOptions["FA0001"]);
            Assert.Equal(ReportDiagnostic.Warn, mergedRuleSet.SpecificDiagnosticOptions["FA0002"]);
            Assert.Equal(ReportDiagnostic.Error, mergedRuleSet.SpecificDiagnosticOptions["FA0003"]);
        }

        [Fact]
        public void Constructor_SkipNullFile()
        {
            var ruleSetA = new RuleSet("FilePath.fake.a", ReportDiagnostic.Default, new Dictionary<string, ReportDiagnostic>()
            {
                {"FA0001", ReportDiagnostic.Default},
                {"FA0002", ReportDiagnostic.Warn}
            }.ToImmutableDictionary(), ImmutableArray.Create<RuleSetInclude>());
            var ruleSetB = new RuleSet("FilePath.fake.b", ReportDiagnostic.Warn, new Dictionary<string, ReportDiagnostic>()
            {
                {"FA0001", ReportDiagnostic.Suppress},
                {"FA0003", ReportDiagnostic.Error}
            }.ToImmutableDictionary(), ImmutableArray.Create<RuleSetInclude>());

            var mergedRuleSet = new MergedRuleSet(new[] { ruleSetA, null, ruleSetB });

            Assert.Equal(ruleSetB.GeneralDiagnosticOption, mergedRuleSet.GeneralDiagnosticOption);
            Assert.Equal(ReportDiagnostic.Suppress, mergedRuleSet.SpecificDiagnosticOptions["FA0001"]);
            Assert.Equal(ReportDiagnostic.Warn, mergedRuleSet.SpecificDiagnosticOptions["FA0002"]);
            Assert.Equal(ReportDiagnostic.Error, mergedRuleSet.SpecificDiagnosticOptions["FA0003"]);
        }
    }
}
