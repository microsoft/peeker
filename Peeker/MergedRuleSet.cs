using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peeker
{
    public class MergedRuleSet
    {
        public ReportDiagnostic GeneralDiagnosticOption { get; set; } = ReportDiagnostic.Default;
        public Dictionary<string, ReportDiagnostic> SpecificDiagnosticOptions { get; set; } = new Dictionary<string, ReportDiagnostic>();

        /// <summary>
        /// Creates an empty MergedRuleSet.
        /// </summary>
        public MergedRuleSet()
        {

        }

        /// <summary>
        /// Merges rules from multiple RuleSet objects. Later RuleSets override earlier ones.
        /// </summary>
        /// <param name="files">A collection of rule sets to merge.</param>
        public MergedRuleSet(IEnumerable<RuleSet> files)
        {
            if (files == null)
            {
                throw new ArgumentNullException(nameof(files));
            }

            foreach (var file in files)
            {
                if (file == null)
                {
                    continue;
                }

                GeneralDiagnosticOption = file.GeneralDiagnosticOption;
                foreach (var pair in file.SpecificDiagnosticOptions)
                {
                    SpecificDiagnosticOptions[pair.Key] = pair.Value;
                }
            }
        }
    }
}
