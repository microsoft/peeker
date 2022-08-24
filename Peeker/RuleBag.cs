using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Sarif;

#nullable enable

namespace Peeker
{
    public class RuleBag
    {
        public HashSet<string> IncludedRuleIds { get; set; } = new();
        public List<ReportingDescriptor> Rules { get; set; } = new();

        private Dictionary<string, int> RuleMapping = new();

        public RuleBag()
        {

        }

        public (int, ReportingDescriptor?) RegisterRule(DiagnosticDescriptor descriptor)
        {
            if (descriptor == null)
            {
                return (-1, null);
            }

            string ruleId = descriptor.Id;
            int index = 0;

            if (IncludedRuleIds.Contains(ruleId))
            {
                index = RuleMapping[ruleId];
                return (index, Rules[index]);
            }

            IncludedRuleIds.Add(ruleId);
            index = Rules.Count;
            var rule = Convert(descriptor);
            Rules.Add(rule);
            RuleMapping[ruleId] = index;

            return (index, rule);
        }

        private ReportingDescriptor Convert(DiagnosticDescriptor descriptor)
        {
            var ret = new ReportingDescriptor()
            {
                Id = descriptor.Id,
            };

            string? shortDescription = descriptor.Title.ToString();
            if (!string.IsNullOrWhiteSpace(shortDescription))
            {
                ret.ShortDescription = new MultiformatMessageString(shortDescription, null, null);
            }

            string? fullDescription = descriptor.Description.ToString();
            if (!string.IsNullOrWhiteSpace(fullDescription))
            {
                ret.FullDescription = new MultiformatMessageString(fullDescription, null, null);
            }

            if (!string.IsNullOrWhiteSpace(descriptor.HelpLinkUri) &&
                Uri.TryCreate(descriptor.HelpLinkUri, UriKind.RelativeOrAbsolute, out Uri? helpUri))
            {
                ret.HelpUri = helpUri;
            }

            if (!string.IsNullOrEmpty(descriptor.Category))
            {
                ret.SetProperty("category", descriptor.Category);
            }

            if (descriptor.CustomTags.Any())
            {
                ret.SetProperty("tags", descriptor.CustomTags.ToArray());
            }

            FailureLevel defaultLevel = GetLevel(descriptor.DefaultSeverity);

            // Don't bother to emit default values.
            bool emitLevel = defaultLevel != FailureLevel.Warning;

            // The default value for "enabled" is "true".
            bool emitEnabled = !descriptor.IsEnabledByDefault;

            if (emitLevel || emitEnabled)
            {
                ret.DefaultConfiguration = new ReportingConfiguration(descriptor.IsEnabledByDefault, defaultLevel, -1, null, null);
            }

            return ret;
        }
        public static FailureLevel GetLevel(DiagnosticSeverity severity)
        {
            switch (severity)
            {
                case DiagnosticSeverity.Info:
                    return FailureLevel.Note;

                case DiagnosticSeverity.Error:
                    return FailureLevel.Error;

                case DiagnosticSeverity.Warning:
                    return FailureLevel.Warning;

                case DiagnosticSeverity.Hidden:
                default:
                    goto case DiagnosticSeverity.Warning;
            }
        }
    }
}
