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

using Microsoft.CodeAnalysis.Testing;

using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

using NUnit.Framework;

using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixTest<
    FluentMigrator.Analyzers.NotNullableOrNullableAnalyzer,
    FluentMigrator.Analyzers.NotNullableOrNullableCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace FluentMigrator.Analyzers.Tests
{
    [TestFixture]
    public class NotNullableOrNullableCodeFixProviderTests
    {
        private static DiagnosticResult GetExpectedDiagnostic()
            => new DiagnosticResult(NotNullableOrNullableAnalyzer.DiagnosticId, DiagnosticSeverity.Info);

        private static VerifyCS GetCodeFixVerifier(string testCode, string fixedCode, params DiagnosticResult[] expectedDiagnostics)
        {
            var verifier = new VerifyCS
            {
                TestCode = testCode,
                FixedCode = fixedCode,
            };
            verifier.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);
            verifier.TestState.AdditionalReferences.Add(typeof(Migration).Assembly);
            verifier.TestState.AdditionalReferences.Add(typeof(IMigration).Assembly);

            return verifier;
        }

        [Test]
        public async Task ColumnWithoutNotNullableOrNullable_ShouldApplyCodeFix()
        {
            //language=csharp
            const string testCode = @"using FluentMigrator;

public class MigrationTest : ForwardOnlyMigration
{
    public override void Up()
    {
        Create.Table(""Users"")
            .WithColumn(""Name"").AsString()
            .WithColumn(""Login"").AsString().NotNullable();
    }
}";

            //language=csharp
            const string fixedCode = @"using FluentMigrator;

public class MigrationTest : ForwardOnlyMigration
{
    public override void Up()
    {
        Create.Table(""Users"")
            .WithColumn(""Name"").AsString().NotNullable()
            .WithColumn(""Login"").AsString().NotNullable();
    }
}";

            var expectedDiagnostic = GetExpectedDiagnostic()
                .WithSpan(8, 25, 8, 31)
                .WithArguments("\"Name\"");

            await GetCodeFixVerifier(testCode, fixedCode, expectedDiagnostic).RunAsync();
        }

        [Test]
        public async Task ColumnWithNotNullable_ShouldNotReportDiagnostic()
        {
            //language=csharp
            const string testCode = @"using FluentMigrator;

public class MigrationTest : ForwardOnlyMigration
{
    public override void Up()
    {
        Create.Table(""Users"")
            .WithColumn(""Name"").AsString().NotNullable()
            .WithColumn(""Login"").AsString().NotNullable();
    }
}";

            await GetCodeFixVerifier(testCode, null).RunAsync();
        }

        [Test]
        public async Task ColumnWithNullable_ShouldNotReportDiagnostic()
        {
            //language=csharp
            const string testCode = @"using FluentMigrator;

public class MigrationTest : ForwardOnlyMigration
{
    public override void Up()
    {
        Create.Table(""Users"")
            .WithColumn(""Name"").AsString().Nullable()
            .WithColumn(""Login"").AsString().NotNullable();
    }
}";

            await GetCodeFixVerifier(testCode, null).RunAsync();
        }

        [Test]
        public async Task MultipleColumnsWithoutNotNullableOrNullable_ShouldApplyCodeFix()
        {
            //language=csharp
            const string testCode = @"using FluentMigrator;

public class MigrationTest : ForwardOnlyMigration
{
    public override void Up()
    {
        Create.Table(""Users"")
            .WithColumn(""Name"").AsString()
            .WithColumn(""Login"").AsString()
            .WithColumn(""Password"").AsString().NotNullable();
    }
}";

            //language=csharp
            const string fixedCode = @"using FluentMigrator;

public class MigrationTest : ForwardOnlyMigration
{
    public override void Up()
    {
        Create.Table(""Users"")
            .WithColumn(""Name"").AsString().NotNullable()
            .WithColumn(""Login"").AsString().NotNullable()
            .WithColumn(""Password"").AsString().NotNullable();
    }
}";

            var expectedDiagnostics = new[]
            {
                GetExpectedDiagnostic()
                    .WithSpan(8, 25, 8, 31)
                    .WithArguments("\"Name\""),
                GetExpectedDiagnostic()
                    .WithSpan(9, 25, 9, 32)
                    .WithArguments("\"Login\"")
            };

            await GetCodeFixVerifier(testCode, fixedCode, expectedDiagnostics).RunAsync();
        }

        [Test]
        public async Task TableCreationWithForeignKey_ColumnsWithoutNotNullableOrNullable_ShouldApplyCodeFix()
        {
            //language=csharp
            const string testCode = @"using FluentMigrator;

public class MigrationTest : ForwardOnlyMigration
{
    public override void Up()
    {
        Create.Table(""Orders"")
            .WithColumn(""OrderId"").AsInt32().PrimaryKey().Identity()
            .WithColumn(""UserId"").AsInt32()
            .WithColumn(""OrderDate"").AsDateTime().NotNullable()
            .WithColumn(""Total"").AsDecimal().NotNullable();

        Create.ForeignKey()
            .FromTable(""Orders"").ForeignColumn(""UserId"")
            .ToTable(""Users"").PrimaryColumn(""UserId"");
    }
}";

            //language=csharp
            const string fixedCode = @"using FluentMigrator;

public class MigrationTest : ForwardOnlyMigration
{
    public override void Up()
    {
        Create.Table(""Orders"")
            .WithColumn(""OrderId"").AsInt32().PrimaryKey().Identity()
            .WithColumn(""UserId"").AsInt32().NotNullable()
            .WithColumn(""OrderDate"").AsDateTime().NotNullable()
            .WithColumn(""Total"").AsDecimal().NotNullable();

        Create.ForeignKey()
            .FromTable(""Orders"").ForeignColumn(""UserId"")
            .ToTable(""Users"").PrimaryColumn(""UserId"");
    }
}";

            var expectedDiagnostic = GetExpectedDiagnostic()
                .WithSpan(9, 25, 9, 33)
                .WithArguments("\"UserId\"");

            await GetCodeFixVerifier(testCode, fixedCode, expectedDiagnostic).RunAsync();
        }

        [Test]
        public async Task TableAlterationWithColumnChange_ShouldApplyCodeFix()
        {
            //language=csharp
            const string testCode = @"using FluentMigrator;

public class MigrationTest : ForwardOnlyMigration
{
    public override void Up()
    {
        Alter.Table(""Users"")
            .AddColumn(""Email"").AsString();
    }
}";

            //language=csharp
            const string fixedCode = @"using FluentMigrator;

public class MigrationTest : ForwardOnlyMigration
{
    public override void Up()
    {
        Alter.Table(""Users"")
            .AddColumn(""Email"").AsString().NotNullable();
    }
}";

            var expectedDiagnostic = GetExpectedDiagnostic()
                .WithSpan(8, 24, 8, 31)
                .WithArguments("\"Email\"");

            await GetCodeFixVerifier(testCode, fixedCode, expectedDiagnostic).RunAsync();
        }

        [Test]
        public async Task TableAlterationWithColumnChange_ShouldNotReportDiagnostic()
        {
            //language=csharp
            const string testCode = @"using FluentMigrator;

public class Migration : ForwardOnlyMigration
{
    public override void Up()
    {
        Alter.Table(""Users"")
            .AddColumn(""Email"").AsString().NotNullable();
    }
}";

            await GetCodeFixVerifier(testCode, null).RunAsync();
        }

        [Test]
        public async Task TableAlterationWithMultipleColumnChanges_ShouldApplyCodeFix()
        {
            //language=csharp
            const string testCode = @"using FluentMigrator;

public class Migration : ForwardOnlyMigration
{
    public override void Up()
    {
        Alter.Table(""Users"")
            .AddColumn(""Email"").AsString()
            .AddColumn(""PhoneNumber"").AsString();
    }
}";

            //language=csharp
            const string fixedCode = @"using FluentMigrator;

public class Migration : ForwardOnlyMigration
{
    public override void Up()
    {
        Alter.Table(""Users"")
            .AddColumn(""Email"").AsString().NotNullable()
            .AddColumn(""PhoneNumber"").AsString().NotNullable();
    }
}";

            var expectedDiagnostics = new[]
            {
                GetExpectedDiagnostic()
                    .WithSpan(8, 24, 8, 31)
                    .WithArguments("\"Email\""),
                GetExpectedDiagnostic()
                    .WithSpan(9, 24, 9, 37)
                    .WithArguments("\"PhoneNumber\"")
            };

            await GetCodeFixVerifier(testCode, fixedCode, expectedDiagnostics).RunAsync();
        }

        [Test]
        public async Task TableCreationWithAllColumnsConfiguredCorrectly_ShouldNotReportDiagnostic()
        {
            //language=csharp
            const string testCode = @"using FluentMigrator;

public class Migration : ForwardOnlyMigration
{
    public override void Up()
    {
        Create.Table(""Products"")
            .WithColumn(""ProductId"").AsInt32().PrimaryKey().Identity()
            .WithColumn(""ProductName"").AsString().NotNullable()
            .WithColumn(""Price"").AsDecimal().NotNullable()
            .WithColumn(""StockQuantity"").AsInt32().NotNullable();
    }
}";

            await GetCodeFixVerifier(testCode, null).RunAsync();
        }

        [Test]
        public async Task TableCreationWithForeignKey_AllColumnsConfiguredCorrectly_ShouldNotReportDiagnostic()
        {
            //language=csharp
            const string testCode = @"using FluentMigrator;

public class Migration : ForwardOnlyMigration
{
    public override void Up()
    {
        Create.Table(""Orders"")
            .WithColumn(""OrderId"").AsInt32().PrimaryKey().Identity()
            .WithColumn(""UserId"").AsInt32().NotNullable()
            .WithColumn(""OrderDate"").AsDateTime().NotNullable()
            .WithColumn(""Total"").AsDecimal().NotNullable();

        Create.ForeignKey()
            .FromTable(""Orders"").ForeignColumn(""UserId"")
            .ToTable(""Users"").PrimaryColumn(""UserId"");
    }
}";

            await GetCodeFixVerifier(testCode, null).RunAsync();
        }
    }
}
