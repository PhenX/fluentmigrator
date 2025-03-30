#region License
//
// Copyright (c) 2007-2024, Fluent Migrator Project
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FluentMigrator.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NotNullableOrNullableAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "FM0003";
        private const string Category = "FluentMigrator";

        private static readonly LocalizableString Title = "NotNullable, Nullable or PrimaryKey required";
        private static readonly LocalizableString MessageFormat = "Column '{0}' must specify NotNullable(), Nullable() or PrimaryKey()";
        private static readonly LocalizableString Description = "Columns must specify NotNullable(), Nullable() or PrimaryKey() after specifying the type";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
        {
            var invocationExpr = (InvocationExpressionSyntax)context.Node;

            if (!IsColumnMethod(invocationExpr))
            {
                return;
            }

            var columnNameArgument = GetColumnNameArgument(invocationExpr);
            if (columnNameArgument != null && !HasNotNullableOrNullable(invocationExpr))
            {
                var diagnostic = Diagnostic.Create(Rule, columnNameArgument.GetLocation(), columnNameArgument.ToString());
                context.ReportDiagnostic(diagnostic);
            }
        }

        private bool IsColumnMethod(InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var methodName = memberAccess.Name.Identifier.Text;
                return methodName == "WithColumn" || methodName == "AddColumn" || methodName == "AlterColumn";
            }
            return false;
        }

        private bool HasNotNullableOrNullable(InvocationExpressionSyntax invocation)
        {
            var current = invocation.Parent;
            while (current != null)
            {
                if (current is InvocationExpressionSyntax parentInvocation &&
                    parentInvocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    var methodName = memberAccess.Name.Identifier.Text;

                    if (methodName == "NotNullable" || methodName == "Nullable" || methodName == "PrimaryKey")
                    {
                        return true;
                    }

                    if (IsColumnMethod(parentInvocation))
                    {
                        return false; // Stop searching if another column method is found
                    }
                }
                current = current.Parent;
            }
            return false;
        }

        private static ArgumentSyntax GetColumnNameArgument(InvocationExpressionSyntax invocation)
        {
            return invocation.ArgumentList.Arguments.Count > 0 ? invocation.ArgumentList.Arguments[0] : null;
        }
    }
}
