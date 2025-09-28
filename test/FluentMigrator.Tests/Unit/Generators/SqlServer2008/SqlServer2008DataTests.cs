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

using FluentMigrator.Runner.Generators.SqlServer;
using FluentMigrator.Tests.Unit.Generators.SqlServer2000;
using NUnit.Framework;
using Shouldly;

namespace FluentMigrator.Tests.Unit.Generators.SqlServer2008
{
    [TestFixture]
    [Category("SqlServer2008")]
    public class SqlServer2008DataTests : SqlServer2000DataTests
    {
        [SetUp]
        public new void Setup()
        {
            Generator = new SqlServer2008Generator();
        }

        [Test]
        public override void CanUpsertDataWithSingleMatchColumnAndCustomSchema()
        {
            var expression = GeneratorTestHelper.GetUpsertDataExpression();
            expression.SchemaName = "TestSchema";

            var result = Generator.Generate(expression);
            result.ShouldContain("MERGE [TestSchema].[TestTable1] AS target");
            result.ShouldContain("USING (VALUES (N'Just''in', N'github.com')) AS source ([Name], [Website])");
            result.ShouldContain("ON (target.[Name] = source.[Name])");
            result.ShouldContain("WHEN MATCHED THEN");
            result.ShouldContain("UPDATE SET [Website] = source.[Website]");
            result.ShouldContain("WHEN NOT MATCHED THEN");
            result.ShouldContain("INSERT ([Name], [Website])");
            result.ShouldContain("VALUES (source.[Name], source.[Website])");
        }

        [Test]
        public override void CanUpsertDataWithSingleMatchColumnAndDefaultSchema()
        {
            var expression = GeneratorTestHelper.GetUpsertDataExpression();

            var result = Generator.Generate(expression);
            result.ShouldContain("MERGE [TestTable1] AS target");
            result.ShouldContain("USING (VALUES (N'Just''in', N'github.com')) AS source ([Name], [Website])");
            result.ShouldContain("ON (target.[Name] = source.[Name])");
            result.ShouldContain("WHEN MATCHED THEN");
            result.ShouldContain("UPDATE SET [Website] = source.[Website]");
            result.ShouldContain("WHEN NOT MATCHED THEN");
            result.ShouldContain("INSERT ([Name], [Website])");
            result.ShouldContain("VALUES (source.[Name], source.[Website])");
        }

        [Test]
        public override void CanUpsertDataWithMultipleMatchColumnsAndCustomSchema()
        {
            var expression = GeneratorTestHelper.GetUpsertDataExpressionWithMultipleMatchColumns();
            expression.SchemaName = "TestSchema";

            var result = Generator.Generate(expression);
            result.ShouldContain("MERGE [TestSchema].[TestTable1] AS target");
            result.ShouldContain("USING (VALUES (N'Just''in', N'Developer', N'github.com')) AS source ([Name], [Category], [Website])");
            result.ShouldContain("ON (target.[Name] = source.[Name] AND target.[Category] = source.[Category])");
            result.ShouldContain("WHEN MATCHED THEN");
            result.ShouldContain("UPDATE SET [Website] = source.[Website]");
            result.ShouldContain("WHEN NOT MATCHED THEN");
            result.ShouldContain("INSERT ([Name], [Category], [Website])");
            result.ShouldContain("VALUES (source.[Name], source.[Category], source.[Website])");
        }

        [Test]
        public override void CanUpsertDataWithMultipleMatchColumnsAndDefaultSchema()
        {
            var expression = GeneratorTestHelper.GetUpsertDataExpressionWithMultipleMatchColumns();

            var result = Generator.Generate(expression);
            result.ShouldContain("MERGE [TestTable1] AS target");
            result.ShouldContain("ON (target.[Name] = source.[Name] AND target.[Category] = source.[Category])");
        }

        [Test]
        public override void CanUpsertDataWithSpecificUpdateColumns()
        {
            var expression = GeneratorTestHelper.GetUpsertDataExpressionWithSpecificUpdateColumns();

            var result = Generator.Generate(expression);
            result.ShouldContain("MERGE [TestTable1] AS target");
            result.ShouldContain("WHEN MATCHED THEN");
            result.ShouldContain("UPDATE SET [Website] = source.[Website]");
            result.ShouldNotContain("UPDATE SET [Email] = source.[Email]"); // Should not update non-specified columns
            result.ShouldContain("INSERT ([Name], [Website], [Email])"); // But insert should include all columns
        }

        [Test]
        public override void CanUpsertMultipleRows()
        {
            var expression = GeneratorTestHelper.GetUpsertDataExpressionWithMultipleRows();

            var result = Generator.Generate(expression);
            // Should generate separate MERGE statements for each row
            result.ShouldContain("MERGE [TestTable1] AS target");
            result.ShouldContain("USING (VALUES (N'Just''in', N'github.com')) AS source");
            result.ShouldContain("USING (VALUES (N'Jane', N'example.com')) AS source");
        }

        [Test]
        public void ShouldGenerateCorrectMergeStatementTermination()
        {
            var expression = GeneratorTestHelper.GetUpsertDataExpression();

            var result = Generator.Generate(expression);
            result.ShouldEndWith(";");
        }

        [Test]
        public void ShouldQuoteSpecialCharactersInMergeStatement()
        {
            var expression = GeneratorTestHelper.GetUpsertDataExpression();

            var result = Generator.Generate(expression);
            result.ShouldContain("N'Just''in'"); // Should properly escape single quotes
        }
    }
}