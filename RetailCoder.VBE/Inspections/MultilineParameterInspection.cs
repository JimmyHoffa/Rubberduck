using System.Collections.Generic;
using System.Linq;
using Rubberduck.Parsing;
using Rubberduck.Parsing.Symbols;
using Rubberduck.Parsing.VBA;
using Rubberduck.UI;

namespace Rubberduck.Inspections
{
    public sealed class MultilineParameterInspection : InspectionBase
    {
        public MultilineParameterInspection(RubberduckParserState state)
            : base(state)
        {
            Severity = CodeInspectionSeverity.Warning;
        }

        public override string Description { get { return RubberduckUI.MultilineParameter_; } }
        public override CodeInspectionType InspectionType { get { return CodeInspectionType.MaintainabilityAndReadabilityIssues; } }

        public override IEnumerable<CodeInspectionResultBase> GetInspectionResults()
        {
            var multilineParameters = from p in UserDeclarations
                .Where(item => item.DeclarationType == DeclarationType.Parameter)
                where p.Context.GetSelection().LineCount > 1
                select p;

            var issues = multilineParameters
                .Select(param => new MultilineParameterInspectionResult(this, string.Format(param.Context.GetSelection().LineCount > 3 ? RubberduckUI.EasterEgg_Continuator : Description, param.IdentifierName), param.Context, param.QualifiedName));

            return issues;
        }
    }
}