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
using FluentMigrator.SqlServer;
using NUnit.Framework;
using Shouldly;

namespace FluentMigrator.Tests.Unit.Generators.SqlServer2008
{
    [TestFixture]
    [Category("SqlServer2008")]
    public class SqlServer2008DataTests : BaseDataTests
    {
        protected SqlServer2008Generator Generator;

        [SetUp]
        public void Setup()
        {
            Generator = new SqlServer2008Generator();
        }

        [Test]
        public override void CanDeleteDataForAllRowsWithCustomSchema()
        {
            var expression = GeneratorTestHelper.GetDeleteDataAllRowsExpression();
            expression.SchemaName = "TestSchema";

            var result = Generator.Generate(expression);
            result.ShouldBe("DELETE FROM [TestTable1] WHERE 1 = 1;");
        }

        [Test]
        public override void CanDeleteDataForAllRowsWithDefaultSchema()
        {
            var expression = GeneratorTestHelper.GetDeleteDataAllRowsExpression();

            var result = Generator.Generate(expression);
            result.ShouldBe("DELETE FROM [TestTable1] WHERE 1 = 1;");
        }

        [Test]
        public override void CanDeleteDataForMultipleRowsWithCustomSchema()
        {
            var expression = GeneratorTestHelper.GetDeleteDataMultipleRowsExpression();
            expression.SchemaName = "TestSchema";

            var result = Generator.Generate(expression);
            result.ShouldBe("DELETE FROM [TestTable1] WHERE [Name] = N'Just''in' AND [Website] IS NULL;DELETE FROM [TestTable1] WHERE [Website] = N'github.com';");
        }

        [Test]
        public override void CanDeleteDataForMultipleRowsWithDefaultSchema()
        {
            var expression = GeneratorTestHelper.GetDeleteDataMultipleRowsExpression();

            var result = Generator.Generate(expression);
            result.ShouldBe("DELETE FROM [TestTable1] WHERE [Name] = N'Just''in' AND [Website] IS NULL;DELETE FROM [TestTable1] WHERE [Website] = N'github.com';");
        }

        [Test]
        public override void CanDeleteDataWithCustomSchema()
        {
            var expression = GeneratorTestHelper.GetDeleteDataExpression();
            expression.SchemaName = "TestSchema";

            var result = Generator.Generate(expression);
            result.ShouldBe("DELETE FROM [TestTable1] WHERE [Name] = N'Just''in' AND [Website] IS NULL;");
        }

        [Test]
        public override void CanDeleteDataWithDefaultSchema()
        {
            var expression = GeneratorTestHelper.GetDeleteDataExpression();

            var result = Generator.Generate(expression);
            result.ShouldBe("DELETE FROM [TestTable1] WHERE [Name] = N'Just''in' AND [Website] IS NULL;");
        }

        [Test]
        public override void CanDeleteDataWithDbNullCriteria()
        {
            var expression = GeneratorTestHelper.GetDeleteDataExpressionWithDbNullValue();
            var result = Generator.Generate(expression);
            result.ShouldBe("DELETE FROM [TestTable1] WHERE [Name] = N'Just''in' AND [Website] IS NULL;");
        }

        [Test]
        public override void CanInsertDataWithCustomSchema()
        {
            var expression = GeneratorTestHelper.GetInsertDataExpression();
            expression.SchemaName = "TestSchema";

            var expected = "INSERT INTO [TestTable1] ([Id], [Name], [Website]) VALUES (1, N'Just''in', N'codethinked.com');";
            expected += @"INSERT INTO [TestTable1] ([Id], [Name], [Website]) VALUES (2, N'Na\te', N'kohari.org');";

            var result = Generator.Generate(expression);
            result.ShouldBe(expected);
        }

        [Test]
        public override void CanInsertDataWithDefaultSchema()
        {
            var expression = GeneratorTestHelper.GetInsertDataExpression();

            var expected = "INSERT INTO [TestTable1] ([Id], [Name], [Website]) VALUES (1, N'Just''in', N'codethinked.com');";
            expected += @"INSERT INTO [TestTable1] ([Id], [Name], [Website]) VALUES (2, N'Na\te', N'kohari.org');";

            var result = Generator.Generate(expression);
            result.ShouldBe(expected);
        }

        [Test]
        public override void CanInsertGuidDataWithCustomSchema()
        {
            var expression = GeneratorTestHelper.GetInsertGUIDExpression();
            expression.SchemaName = "TestSchema";

            var result = Generator.Generate(expression);
            result.ShouldBe(string.Format("INSERT INTO [TestTable1] ([guid]) VALUES ('{0}');", GeneratorTestHelper.TestGuid.ToString()));
        }

        [Test]
        public override void CanInsertGuidDataWithDefaultSchema()
        {
            var expression = GeneratorTestHelper.GetInsertGUIDExpression();

            var result = Generator.Generate(expression);
            result.ShouldBe(string.Format("INSERT INTO [TestTable1] ([guid]) VALUES ('{0}');", GeneratorTestHelper.TestGuid.ToString()));
        }

        [Test]
        public override void CanUpdateDataForAllDataWithCustomSchema()
        {
            var expression = GeneratorTestHelper.GetUpdateDataExpressionWithAllRows();
            expression.SchemaName = "TestSchema";

            var result = Generator.Generate(expression);
            result.ShouldBe("UPDATE [TestTable1] SET [Name] = N'Just''in', [Age] = 25 WHERE 1 = 1;");
        }

        [Test]
        public override void CanUpdateDataForAllDataWithDefaultSchema()
        {
            var expression = GeneratorTestHelper.GetUpdateDataExpressionWithAllRows();

            var result = Generator.Generate(expression);
            result.ShouldBe("UPDATE [TestTable1] SET [Name] = N'Just''in', [Age] = 25 WHERE 1 = 1;");
        }

        [Test]
        public override void CanUpdateDataWithCustomSchema()
        {
            var expression = GeneratorTestHelper.GetUpdateDataExpression();
            expression.SchemaName = "TestSchema";

            var result = Generator.Generate(expression);
            result.ShouldBe("UPDATE [TestTable1] SET [Name] = N'Just''in', [Age] = 25 WHERE [Id] = 9 AND [Homepage] IS NULL;");
        }

        [Test]
        public override void CanUpdateDataWithDefaultSchema()
        {
            var expression = GeneratorTestHelper.GetUpdateDataExpression();

            var result = Generator.Generate(expression);
            result.ShouldBe("UPDATE [TestTable1] SET [Name] = N'Just''in', [Age] = 25 WHERE [Id] = 9 AND [Homepage] IS NULL;");
        }

        [Test]
        public override void CanUpdateDataWithDbNullCriteria()
        {
            var expression = GeneratorTestHelper.GetUpdateDataExpressionWithDbNullValue();

            var result = Generator.Generate(expression);
            result.ShouldBe("UPDATE [dbo].[TestTable1] SET [Name] = N'Just''in', [Age] = 25 WHERE [Id] = 9 AND [Homepage] IS NULL;");
        }

        [Test]
        public void CanInsertDataWithIdentityInsert()
        {
            var expression = GeneratorTestHelper.GetInsertDataExpression();
            expression.AdditionalFeatures.Add(SqlServerExtensions.IdentityInsert, true);

            var expected = "SET IDENTITY_INSERT [TestTable1] ON;";
            expected += "INSERT INTO [dbo].[TestTable1] ([Id], [Name], [Website]) VALUES (1, N'Just''in', N'codethinked.com');";
            expected += @"INSERT INTO [dbo].[TestTable1] ([Id], [Name], [Website]) VALUES (2, N'Na\te', N'kohari.org');";
            expected += "SET IDENTITY_INSERT [TestTable1] OFF;";

            var result = Generator.Generate(expression);
            result.ShouldBe(expected);
        }

        [Test]
        public void CanInsertDataWithIdentityInsertInStrictMode()
        {
            var expression = GeneratorTestHelper.GetInsertDataExpression();
            expression.AdditionalFeatures.Add(SqlServerExtensions.IdentityInsert, true);
            Generator.CompatibilityMode = Runner.CompatibilityMode.STRICT;

            var expected = "SET IDENTITY_INSERT [TestTable1] ON;";
            expected += "INSERT INTO [TestTable1] ([Id], [Name], [Website]) VALUES (1, N'Just''in', N'codethinked.com');";
            expected += @"INSERT INTO [TestTable1] ([Id], [Name], [Website]) VALUES (2, N'Na\te', N'kohari.org');";
            expected += "SET IDENTITY_INSERT [TestTable1] OFF;";

            var result = Generator.Generate(expression);
            result.ShouldBe(expected);
        }

        [Test]
        public void CanUpsertDataWithSingleMatchColumnAndCustomSchema()
        {
            var expression = GeneratorTestHelper.GetUpsertDataExpression();
            expression.SchemaName = "TestSchema";

            var result = Generator.Generate(expression);
            result.ShouldBe("""
                            MERGE [TestSchema].[TestTable1] AS target
                            USING (VALUES (N'Just''in', N'github.com')) AS source ([Name], [Website])
                            ON (target.[Name] = source.[Name])
                            WHEN MATCHED THEN
                                UPDATE SET [Website] = source.[Website]
                            WHEN NOT MATCHED THEN
                                INSERT ([Name], [Website])
                                VALUES (source.[Name], source.[Website])
                            ;
                            """);
        }

        [Test]
        public void CanUpsertDataWithSingleMatchColumnAndDefaultSchema()
        {
            var expression = GeneratorTestHelper.GetUpsertDataExpression();

            var result = Generator.Generate(expression);
            result.ShouldBe("""
                            MERGE [dbo].[TestTable1] AS target
                            USING (VALUES (N'Just''in', N'github.com')) AS source ([Name], [Website])
                            ON (target.[Name] = source.[Name])
                            WHEN MATCHED THEN
                                UPDATE SET [Website] = source.[Website]
                            WHEN NOT MATCHED THEN
                                INSERT ([Name], [Website])
                                VALUES (source.[Name], source.[Website])
                            ;
                            """);
        }

        [Test]
        public void CanUpsertDataWithMultipleMatchColumnsAndCustomSchema()
        {
            var expression = GeneratorTestHelper.GetUpsertDataExpressionWithMultipleMatchColumns();
            expression.SchemaName = "TestSchema";

            var result = Generator.Generate(expression);
            result.ShouldBe("""
                            MERGE [TestSchema].[TestTable1] AS target
                            USING (VALUES (N'Just''in', N'Developer', N'github.com')) AS source ([Name], [Category], [Website])
                            ON (target.[Name] = source.[Name] AND target.[Category] = source.[Category])
                            WHEN MATCHED THEN
                                UPDATE SET [Website] = source.[Website]
                            WHEN NOT MATCHED THEN
                                INSERT ([Name], [Category], [Website])
                                VALUES (source.[Name], source.[Category], source.[Website])
                            ;
                            """);
        }

        [Test]
        public void CanUpsertDataWithMultipleMatchColumnsAndDefaultSchema()
        {
            var expression = GeneratorTestHelper.GetUpsertDataExpressionWithMultipleMatchColumns();

            var result = Generator.Generate(expression);
            result.ShouldBe("""
                            MERGE [dbo].[TestTable1] AS target
                            USING (VALUES (N'Just''in', N'Developer', N'github.com')) AS source ([Name], [Category], [Website])
                            ON (target.[Name] = source.[Name] AND target.[Category] = source.[Category])
                            WHEN MATCHED THEN
                                UPDATE SET [Website] = source.[Website]
                            WHEN NOT MATCHED THEN
                                INSERT ([Name], [Category], [Website])
                                VALUES (source.[Name], source.[Category], source.[Website])
                            ;
                            """);
        }

        [Test]
        public void CanUpsertDataWithSpecificUpdateColumns()
        {
            var expression = GeneratorTestHelper.GetUpsertDataExpressionWithSpecificUpdateColumns();

            var result = Generator.Generate(expression);
            result.ShouldBe("""
                            MERGE [dbo].[TestTable1] AS target
                            USING (VALUES (N'Just''in', N'github.com', N'test@example.com')) AS source ([Name], [Website], [Email])
                            ON (target.[Name] = source.[Name])
                            WHEN MATCHED THEN
                                UPDATE SET [Website] = source.[Website]
                            WHEN NOT MATCHED THEN
                                INSERT ([Name], [Website], [Email])
                                VALUES (source.[Name], source.[Website], source.[Email])
                            ;
                            """);
        }

        [Test]
        public void CanUpsertMultipleRows()
        {
            var expression = GeneratorTestHelper.GetUpsertDataExpressionWithMultipleRows();

            var result = Generator.Generate(expression);
            result.ShouldBe("""
                            MERGE [dbo].[TestTable1] AS target
                            USING (VALUES
                                (N'Just''in', N'github.com'),
                                (N'Jane', N'example.com')
                            ) AS source ([Name], [Website])
                            ON (target.[Name] = source.[Name])
                            WHEN MATCHED THEN
                                UPDATE SET target.[Website] = source.[Website]
                            WHEN NOT MATCHED THEN
                                INSERT ([Name], [Website])
                                VALUES (source.[Name], source.[Website])
                            ;
                            """, StringCompareShould.IgnoreLineEndings);
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
            result.ShouldBe("""
                            MERGE [dbo].[TestTable1] AS target
                            USING (VALUES (N'Just''in', N'github.com')) AS source ([Name], [Website])
                            ON (target.[Name] = source.[Name])
                            WHEN MATCHED THEN
                                UPDATE SET [Website] = source.[Website]
                            WHEN NOT MATCHED THEN
                                INSERT ([Name], [Website])
                                VALUES (source.[Name], source.[Website])
                            ;
                            """);
        }

        [Test]
        public void CanUpsertDataWithIgnoreInsertIfExists()
        {
            var expression = GeneratorTestHelper.GetUpsertDataExpressionWithIgnoreInsertIfExists();

            var result = Generator.Generate(expression);
            result.ShouldBe("""
                            MERGE [dbo].[TestTable1] AS target
                            USING (VALUES (N'Just''in', N'github.com')) AS source ([Name], [Website])
                            ON (target.[Name] = source.[Name])
                            WHEN NOT MATCHED THEN
                                INSERT ([Name], [Website])
                                VALUES (source.[Name], source.[Website])
                            ;
                            """);
        }

        [Test]
        public void CanUpsertDataWithRawUpdateValues()
        {
            var expression = GeneratorTestHelper.GetUpsertDataExpressionWithRawUpdateValues();

            var result = Generator.Generate(expression);
            result.ShouldBe("""
                            MERGE [dbo].[TestTable1] AS target
                            USING (VALUES (N'Just''in', N'github.com', N'test@example.com')) AS source ([Name], [Website], [Email])
                            ON (target.[Name] = source.[Name])
                            WHEN MATCHED THEN
                                UPDATE SET [Website] = N'codethinked.com', [Email] = UPPER('admin@example.com')
                            WHEN NOT MATCHED THEN
                                INSERT ([Name], [Website], [Email])
                                VALUES (source.[Name], source.[Website], source.[Email])
                            ;
                            """);
        }
    }
}
