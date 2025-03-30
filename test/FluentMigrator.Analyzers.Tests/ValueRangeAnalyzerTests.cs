#region License
// Copyright (c) 2025, Fluent Migrator Project
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

using NUnit.Framework;
using System.Threading.Tasks;

using VerifyTest = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<
    FluentMigrator.Analyzers.ValueRangeAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier
>;

namespace FluentMigrator.Analyzers.Tests
{
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public class ValueRangeAnalyzerTests : VerifyTest
    {
        public ValueRangeAnalyzerTests()
        {
            TestState.AdditionalReferences.Add(typeof(Migration).Assembly);
            TestState.AdditionalReferences.Add(typeof(IMigration).Assembly);
        }

        [Test]
        public async Task PrecisionOutOfRange_ShouldReportDiagnostic()
        {
            //language=csharp
            const string testCode = """
                                    using FluentMigrator;

                                    public class Migration : ForwardOnlyMigration
                                    {
                                        public override void Up()
                                        {
                                            Create.Table("Products")
                                                .WithColumn("Price").AsDecimal(10, 30);
                                        }
                                    }
                                    """;

            var expectedDiagnostic = new DiagnosticResult(ValueRangeAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
                .WithSpan(8, 50, 8, 52)
                .WithArguments(30, "precision", 0, 28);

            TestState.ExpectedDiagnostics.Add(expectedDiagnostic);
            TestState.Sources.Add(testCode);

            await RunAsync();
        }

        [Test]
        public async Task PrecisionWithinRange_ShouldNotReportDiagnostic()
        {
            //language=csharp
            const string testCode = """
                                    using FluentMigrator;

                                    public class Migration : ForwardOnlyMigration
                                    {
                                        public override void Up()
                                        {
                                            Create.Table("Products")
                                                .WithColumn("Price").AsDecimal(10, 10);
                                        }
                                    }
                                    """;
            TestState.Sources.Add(testCode);

            await RunAsync();
        }

        [Test]
        public async Task PrecisionAtLowerBoundary_ShouldNotReportDiagnostic()
        {
            //language=csharp
            const string testCode = """
                                    using FluentMigrator;

                                    public class Migration : ForwardOnlyMigration
                                    {
                                        public override void Up()
                                        {
                                            Create.Table("Products")
                                                .WithColumn("Price").AsDecimal(10, 0);
                                        }
                                    }
                                    """;
            TestState.Sources.Add(testCode);

            await RunAsync();
        }

        [Test]
        public async Task PrecisionAtUpperBoundary_ShouldNotReportDiagnostic()
        {
            //language=csharp
            const string testCode = """
                                    using FluentMigrator;

                                    public class Migration : ForwardOnlyMigration
                                    {
                                        public override void Up()
                                        {
                                            Create.Table("Products")
                                                .WithColumn("Price").AsDecimal(10, 28);
                                        }
                                    }
                                    """;
            TestState.Sources.Add(testCode);

            await RunAsync();
        }

        [Test]
        public async Task MultipleColumnsWithPrecisionOutOfRange_ShouldReportDiagnostics()
        {
            //language=csharp
            const string testCode = """
                                    using FluentMigrator;

                                    public class Migration : ForwardOnlyMigration
                                    {
                                        public override void Up()
                                        {
                                            Create.Table("Products")
                                                .WithColumn("Price").AsDecimal(10, 30)
                                                .WithColumn("Discount").AsDecimal(10, 29);
                                        }
                                    }
                                    """;

            var expectedDiagnostics = new[]
            {
                new DiagnosticResult(ValueRangeAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
                    .WithSpan(8, 50, 8, 52)
                    .WithArguments(30, "precision", 0, 28),
                new DiagnosticResult(ValueRangeAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
                    .WithSpan(9, 54, 9, 56)
                    .WithArguments(29, "precision", 0, 28)
            };

            TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);
            TestState.Sources.Add(testCode);

            await RunAsync();
        }

        [Test]
        public async Task TableAlterationWithPrecisionOutOfRange_ShouldReportDiagnostic()
        {
            //language=csharp
            const string testCode = """
                                    using FluentMigrator;

                                    public class Migration : ForwardOnlyMigration
                                    {
                                        public override void Up()
                                        {
                                            Alter.Table("Products")
                                                .AddColumn("NewPrice").AsDecimal(10, 30);
                                        }
                                    }
                                    """;

            var expectedDiagnostic = new DiagnosticResult(ValueRangeAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
                .WithSpan(8, 53, 8, 55)
                .WithArguments(30, "precision", 0, 28);

            TestState.ExpectedDiagnostics.Add(expectedDiagnostic);
            TestState.Sources.Add(testCode);

            await RunAsync();
        }

        [Test]
        public async Task TableAlterationWithPrecisionWithinRange_ShouldNotReportDiagnostic()
        {
            //language=csharp
            const string testCode = """
                                    using FluentMigrator;

                                    public class Migration : ForwardOnlyMigration
                                    {
                                        public override void Up()
                                        {
                                            Alter.Table("Products")
                                                .AddColumn("NewPrice").AsDecimal(10, 10);
                                        }
                                    }
                                    """;
            TestState.Sources.Add(testCode);

            await RunAsync();
        }
    }
}
