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

using System;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FluentMigrator.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ValueRangeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "FM0002";
        private const string Category = "FluentMigrator";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            title: "Value out of allowed range",
            messageFormat: "Value '{0}' for parameter '{1}' is not in the allowed range {2}-{3}",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeMethodCall,
                SyntaxKind.InvocationExpression,
                SyntaxKind.ObjectCreationExpression);
        }

        private static void AnalyzeMethodCall(SyntaxNodeAnalysisContext context)
        {
            var expression = context.Node;
            var methodSymbol = context.SemanticModel.GetSymbolInfo(expression).Symbol as IMethodSymbol;

            if (methodSymbol == null)
            {
                return;
            }

            var arguments = GetArgumentsFromNode(expression);
            var parameters = methodSymbol.Parameters;

            for (var i = 0; i < arguments.Count; i++)
            {
                var argument = arguments[i];
                var parameter = GetParameterForArgument(parameters, argument, i);

                var rangeAttribute = parameter?.GetAttributes()
                    .FirstOrDefault(a => a.AttributeClass?.Name.StartsWith("ValueRange") == true);

                if (rangeAttribute == null)
                {
                    continue;
                }

                var (min, max) = GetRangeValues(rangeAttribute);
                var argumentValue = context.SemanticModel.GetConstantValue(argument.Expression);

                if (!argumentValue.HasValue || !IsNumeric(argumentValue.Value))
                {
                    continue;
                }

                var value = Convert.ToDecimal(argumentValue.Value);

                if (value < min || value > max)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule,
                        argument.GetLocation(),
                        value,
                        parameter.Name,
                        min,
                        max));
                }
            }
        }

        private static SeparatedSyntaxList<ArgumentSyntax> GetArgumentsFromNode(SyntaxNode node)
        {
            switch (node)
            {
                case InvocationExpressionSyntax invocation:
                    return invocation.ArgumentList.Arguments;
                case ObjectCreationExpressionSyntax creation:
                    return creation.ArgumentList?.Arguments ?? new SeparatedSyntaxList<ArgumentSyntax>();
                default:
                    return new SeparatedSyntaxList<ArgumentSyntax>();
            }
        }

        private static IParameterSymbol GetParameterForArgument(
            ImmutableArray<IParameterSymbol> parameters,
            ArgumentSyntax argument,
            int position)
        {
            if (argument.NameColon == null)
            {
                return position < parameters.Length ? parameters[position] : null;
            }

            var name = argument.NameColon.Name.Identifier.Text;
            return parameters.FirstOrDefault(p => p.Name == name);

        }

        private static (decimal min, decimal max) GetRangeValues(AttributeData attribute)
        {
            if (attribute.ConstructorArguments.Length != 2)
            {
                return (decimal.MinValue, decimal.MaxValue);
            }

            return (
                Convert.ToDecimal(attribute.ConstructorArguments[0].Value),
                Convert.ToDecimal(attribute.ConstructorArguments[1].Value)
            );
        }

        private static bool IsNumeric(object value)
        {
            switch (value)
            {
                case sbyte _:
                case byte _:
                case short _:
                case ushort _:
                case int _:
                case uint _:
                case long _:
                case ulong _:
                case float _:
                case double _:
                case decimal _:
                    return true;
                default:
                    return false;
            }
        }
    }
}
