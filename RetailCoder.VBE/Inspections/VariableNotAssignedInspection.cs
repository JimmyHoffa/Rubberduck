using System.Collections.Generic;
using System.Linq;
using Rubberduck.Parsing.Symbols;
using Rubberduck.Parsing.VBA;
using Rubberduck.UI;

namespace Rubberduck.Inspections
{
    public sealed class VariableNotAssignedInspection : InspectionBase
    {
        public VariableNotAssignedInspection(RubberduckParserState state)
            : base(state)
        {
            Severity = CodeInspectionSeverity.Warning;
        }

        public override string Description { get { return RubberduckUI.VariableNotAssigned_; } }
        public override CodeInspectionType InspectionType { get { return CodeInspectionType.CodeQualityIssues; } }

        public override IEnumerable<CodeInspectionResultBase> GetInspectionResults()
        {
            var items = UserDeclarations.ToList();

            // ignore arrays. todo: ArrayIndicesNotAccessedInspection
            var arrays = items.Where(declaration =>
                declaration.DeclarationType == DeclarationType.Variable
                && declaration.IsArray()).ToList();

            var declarations = items.Where(declaration =>
                declaration.DeclarationType == DeclarationType.Variable
                && !declaration.IsWithEvents
                && !arrays.Contains(declaration)
                && !items.Any(item => 
                    item.IdentifierName == declaration.AsTypeName 
                    && item.DeclarationType == DeclarationType.UserDefinedType) // UDT variables don't need to be assigned
                && !declaration.IsSelfAssigned
                && !declaration.References.Any(reference => reference.IsAssignment));

            return declarations.Select(issue => 
                new IdentifierNotAssignedInspectionResult(this, issue, issue.Context, issue.QualifiedName.QualifiedModuleName));
        }
    }
}