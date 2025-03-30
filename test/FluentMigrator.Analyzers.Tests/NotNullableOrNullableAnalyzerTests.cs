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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

using System.Threading.Tasks;

using NUnit.Framework;

using VerifyTest = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<
    FluentMigrator.Analyzers.NotNullableOrNullableAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier
>;

namespace FluentMigrator.Analyzers.Tests
{
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public class NotNullableOrNullableAnalyzerTests : VerifyTest
    {
        public NotNullableOrNullableAnalyzerTests()
        {
            TestState.AdditionalReferences.Add(typeof(Migration).Assembly);
            TestState.AdditionalReferences.Add(typeof(IMigration).Assembly);
        }

        [Test]
        public async Task ColumnWithoutNotNullableOrNullable_ShouldReportDiagnostic()
        {
            //language=csharp
            const string testCode = """
                                    using FluentMigrator;

                                    public class MigrationTest : ForwardOnlyMigration
                                    {
                                        public override void Up()
                                        {
                                            Create.Table("Users")
                                                .WithColumn("Name").AsString()
                                                .WithColumn("Login").AsString().NotNullable();
                                        }
                                    }
                                    """;

            TestState.ExpectedDiagnostics.Add(new DiagnosticResult(NotNullableOrNullableAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
                .WithSpan(8, 25, 8, 31)
                .WithArguments("\"Name\""));

            TestState.Sources.Add(testCode);

            await RunAsync();
        }

        [Test]
        public async Task ColumnWithNotNullable_ShouldNotReportDiagnostic()
        {
            //language=csharp
            const string testCode = """
                                    using FluentMigrator;

                                    public class MigrationTest : ForwardOnlyMigration
                                    {
                                        public override void Up()
                                        {
                                            Create.Table("Users")
                                                .WithColumn("Name").AsString().NotNullable()
                                                .WithColumn("Login").AsString().NotNullable();
                                        }
                                    }
                                    """;

            TestState.Sources.Add(testCode);

            await RunAsync();
        }

        [Test]
        public async Task ColumnWithNullable_ShouldNotReportDiagnostic()
        {
            //language=csharp
            const string testCode = """
                                    using FluentMigrator;

                                    public class MigrationTest : ForwardOnlyMigration
                                    {
                                        public override void Up()
                                        {
                                            Create.Table("Users")
                                                .WithColumn("Name").AsString().Nullable()
                                                .WithColumn("Login").AsString().NotNullable();
                                        }
                                    }
                                    """;

            TestState.Sources.Add(testCode);

            await RunAsync();
        }

        [Test]
        public async Task MultipleColumnsWithoutNotNullableOrNullable_ShouldReportDiagnostics()
        {
            //language=csharp
            const string testCode = """
                                    using FluentMigrator;

                                    public class MigrationTest : ForwardOnlyMigration
                                    {
                                        public override void Up()
                                        {
                                            Create.Table("Users")
                                                .WithColumn("Name").AsString()
                                                .WithColumn("Login").AsString()
                                                .WithColumn("Password").AsString().NotNullable();
                                        }
                                    }
                                    """;

            var expectedDiagnostics = new[]
            {
                new DiagnosticResult(NotNullableOrNullableAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
                    .WithSpan(8, 25, 8, 31)
                    .WithArguments("\"Name\""),
                new DiagnosticResult(NotNullableOrNullableAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
                    .WithSpan(9, 25, 9, 32)
                    .WithArguments("\"Login\"")
            };

            TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);
            TestState.Sources.Add(testCode);

            await RunAsync();
        }

        [Test]
        public async Task TableCreationWithForeignKey_ColumnsWithoutNotNullableOrNullable_ShouldReportDiagnostics()
        {
            //language=csharp
            const string testCode = """
                                    using FluentMigrator;

                                    public class MigrationTest : ForwardOnlyMigration
                                    {
                                        public override void Up()
                                        {
                                            Create.Table("Orders")
                                                .WithColumn("OrderId").AsInt32().PrimaryKey().Identity()
                                                .WithColumn("UserId").AsInt32()
                                                .WithColumn("OrderDate").AsDateTime().NotNullable()
                                                .WithColumn("Total").AsDecimal().NotNullable();

                                            Create.ForeignKey()
                                                .FromTable("Orders").ForeignColumn("UserId")
                                                .ToTable("Users").PrimaryColumn("UserId");
                                        }
                                    }
                                    """;

            var expectedDiagnostic = new DiagnosticResult(NotNullableOrNullableAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
                .WithSpan(9, 25, 9, 33)
                .WithArguments("\"UserId\"");

            TestState.ExpectedDiagnostics.Add(expectedDiagnostic);
            TestState.Sources.Add(testCode);

            await RunAsync();
        }

        [Test]
        public async Task TableAlterationWithColumnChange_ShouldReportDiagnostic()
        {
            //language=csharp
            const string testCode = """
                                    using FluentMigrator;

                                    public class MigrationTest : ForwardOnlyMigration
                                    {
                                        public override void Up()
                                        {
                                            Alter.Table("Users")
                                                .AddColumn("Email").AsString();
                                        }
                                    }
                                    """;

            var expectedDiagnostic = new DiagnosticResult(NotNullableOrNullableAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
                .WithSpan(8, 24, 8, 31)
                .WithArguments("\"Email\"");

            TestState.ExpectedDiagnostics.Add(expectedDiagnostic);
            TestState.Sources.Add(testCode);

            await RunAsync();
        }

        [Test]
        public async Task TableAlterationWithColumnChange_ShouldNotReportDiagnostic()
        {
            //language=csharp
            const string testCode = """
                                    using FluentMigrator;

                                    public class Migration: ForwardOnlyMigration
                                    {
                                        public override void Up()
                                        {
                                            Alter.Table("Users")
                                                .AddColumn("Email").AsString().NotNullable();
                                        }
                                    }
                                    """;

            TestState.Sources.Add(testCode);

            await RunAsync();
        }

        [Test]
        public async Task TableAlterationWithMultipleColumnChanges_ShouldReportDiagnostics()
        {
            //language=csharp
            const string testCode = """
                                    using FluentMigrator;

                                    public class Migration : ForwardOnlyMigration
                                    {
                                        public override void Up()
                                        {
                                            Alter.Table("Users")
                                                .AddColumn("Email").AsString()
                                                .AddColumn("PhoneNumber").AsString();
                                        }
                                    }
                                    """;

            var expectedDiagnostics = new[]
            {
                new DiagnosticResult(NotNullableOrNullableAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
                    .WithSpan(8, 24, 8, 31)
                    .WithArguments("\"Email\""),
                new DiagnosticResult(NotNullableOrNullableAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
                    .WithSpan(9, 24, 9, 37)
                    .WithArguments("\"PhoneNumber\"")
            };

            TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);
            TestState.Sources.Add(testCode);

            await RunAsync();
        }

        [Test]
        public async Task TableCreationWithAllColumnsConfiguredCorrectly_ShouldNotReportDiagnostic()
        {
            //language=csharp
            const string testCode = """
                                    using FluentMigrator;

                                    public class Migration : ForwardOnlyMigration
                                    {
                                        public override void Up()
                                        {
                                            Create.Table("Products")
                                                .WithColumn("ProductId").AsInt32().PrimaryKey().Identity()
                                                .WithColumn("ProductName").AsString().NotNullable()
                                                .WithColumn("Price").AsDecimal().NotNullable()
                                                .WithColumn("StockQuantity").AsInt32().NotNullable();
                                        }
                                    }
                                    """;

            TestState.Sources.Add(testCode);

            await RunAsync();
        }

        [Test]
        public async Task TableCreationWithForeignKey_AllColumnsConfiguredCorrectly_ShouldNotReportDiagnostic()
        {
            //language=csharp
            const string testCode = """
                                    using FluentMigrator;

                                    public class Migration : ForwardOnlyMigration
                                    {
                                        public override void Up()
                                        {
                                            Create.Table("Orders")
                                                .WithColumn("OrderId").AsInt32().PrimaryKey().Identity()
                                                .WithColumn("UserId").AsInt32().NotNullable()
                                                .WithColumn("OrderDate").AsDateTime().NotNullable()
                                                .WithColumn("Total").AsDecimal().NotNullable();

                                            Create.ForeignKey()
                                                .FromTable("Orders").ForeignColumn("UserId")
                                                .ToTable("Users").PrimaryColumn("UserId");
                                        }
                                    }
                                    """;

            TestState.Sources.Add(testCode);

            await RunAsync();
        }
    }
}
