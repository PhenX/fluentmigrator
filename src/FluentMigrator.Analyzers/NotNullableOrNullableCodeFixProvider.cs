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
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace FluentMigrator.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NotNullableOrNullableCodeFixProvider)), Shared]
    public class NotNullableOrNullableCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Add Nullable() after AsXXX()";

        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(NotNullableOrNullableAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            if (root is null)
            {
                return;
            }

            var diagnostic = context.Diagnostics[0];
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the AsXXX() invocation expression identified by the diagnostic
            var invocationExpr = root.FindToken(diagnosticSpan.Start).Parent?
                .AncestorsAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault(x => ((MemberAccessExpressionSyntax)x.Expression).Name.Identifier.Text.StartsWith("As"));

            if (invocationExpr is null)
            {
                return;
            }

            context.RegisterCodeFix(
                Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
                    title: Title,
                    createChangedDocument: c => AddNullableAsync(context.Document, invocationExpr, c),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static async Task<Document> AddNullableAsync(Document document, InvocationExpressionSyntax invocationExpr, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            // NotNullable() expression with correct trivia
            var memberAccessExpr = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                invocationExpr.WithoutTrailingTrivia(),
                SyntaxFactory.IdentifierName("NotNullable"));

            // Wrap AsXXX() invocation with NotNullable()
            var nullableInvocation = SyntaxFactory.InvocationExpression(memberAccessExpr)
                .WithTrailingTrivia(invocationExpr.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation);

            editor.ReplaceNode(invocationExpr, nullableInvocation);

            return editor.GetChangedDocument();
        }
    }
}
