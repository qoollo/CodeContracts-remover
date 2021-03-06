﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace ContractFix.Special.TurboContractConditionStringIncorrectArg
{
    //[DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TurboContractConditionStringIncorrectArgAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CRS12_TurboContractConditionStringInSeparateParameter";
        private const string Title = "TurboContract call has condition string in message parameter";
        private const string MessageFormat = "TurboContract call has condition string in message parameter";
        private const string Description = "TurboContract call has condition string in message parameter";
        private const string Category = "Usage";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        internal static HashSet<string> MethodNamesToFix { get; } = new HashSet<string>()
        {
            nameof(System.Diagnostics.Contracts.Contract.Requires),
            nameof(System.Diagnostics.Contracts.Contract.Assert),
            nameof(System.Diagnostics.Contracts.Contract.Assume)
        };

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(AnalyzeInvocationOp, OperationKind.Invocation);
        }


        private static bool IsCodeContractToReplace(Compilation compilation, IInvocationOperation invocation)
        {
            if (invocation.TargetMethod.ContainingType.Name != ContractStatementAnalyzer.SpecialContractClass)
                return false;

            if (invocation.TargetMethod.IsGenericMethod)
                return false;

            if (!MethodNamesToFix.Contains(invocation.TargetMethod.Name))
                return false;

            if (invocation.Arguments.Length >= 3)
                return false;

            if (invocation.Arguments.Length == 2)
            {
                if (invocation.Arguments[1].Syntax is ArgumentSyntax argSynt &&
                    (argSynt.NameColon == null || argSynt.NameColon.Name.Identifier.ValueText != "conditionString") &&
                    argSynt.Expression is LiteralExpressionSyntax literal &&
                    literal.IsKind(SyntaxKind.StringLiteralExpression))
                {
                    if (literal.Token.ValueText == invocation.Arguments[0].Syntax.ToString())
                        return true;
                }
            }

            return false;
        }

        private static void AnalyzeInvocationOp(OperationAnalysisContext context)
        {
            var invocation = (IInvocationOperation)context.Operation;
            if (invocation.TargetMethod.Kind != SymbolKind.Method || !invocation.TargetMethod.IsStatic)
                return;
            if (!IsCodeContractToReplace(context.Compilation, invocation))
                return;
            

            var invocationSyntax = (InvocationExpressionSyntax)invocation.Syntax;
            if (invocationSyntax.Expression is MemberAccessExpressionSyntax memberAccess)
                context.ReportDiagnostic(Diagnostic.Create(Rule, invocationSyntax.GetLocation()));
        }
    }
}
