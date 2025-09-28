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

using FluentMigrator.Runner.Generators.MySql;

using NUnit.Framework;

using Shouldly;

namespace FluentMigrator.Tests.Unit.Generators.MySql5
{
    [TestFixture]
    [Category("Generator")]
    [Category("MySql5")]
    public class MySql5DataTests
    {
        protected MySql5Generator Generator;

        [SetUp]
        public void Setup()
        {
            Generator = new MySql5Generator();
        }

        [Test]
        public void CanUpsertData()
        {
            var expression = GeneratorTestHelper.GetUpsertDataExpression();
            
            var result = Generator.Generate(expression);
            result.ShouldBe(
                "INSERT INTO `TestTable1`" + System.Environment.NewLine +
                "(`Name`, `Website`)" + System.Environment.NewLine +
                "VALUES" + System.Environment.NewLine +
                "('Just''in', 'github.com')" + System.Environment.NewLine +
                "ON DUPLICATE KEY UPDATE" + System.Environment.NewLine +
                "    `Website` = VALUES(`Website`);");
        }

        [Test]
        public void CanUpsertDataWithCustomSchema()
        {
            var expression = GeneratorTestHelper.GetUpsertDataExpression();
            expression.SchemaName = "TestSchema";
            
            var result = Generator.Generate(expression);
            result.ShouldBe(
                "INSERT INTO `TestSchema`.`TestTable1`" + System.Environment.NewLine +
                "(`Name`, `Website`)" + System.Environment.NewLine +
                "VALUES" + System.Environment.NewLine +
                "('Just''in', 'github.com')" + System.Environment.NewLine +
                "ON DUPLICATE KEY UPDATE" + System.Environment.NewLine +
                "    `Website` = VALUES(`Website`);");
        }

        [Test]
        public void CanUpsertDataWithSpecificUpdateColumns()
        {
            var expression = GeneratorTestHelper.GetUpsertDataExpressionWithUpdateColumns();
            
            var result = Generator.Generate(expression);
            result.ShouldBe(
                "INSERT INTO `TestTable1`" + System.Environment.NewLine +
                "(`Name`, `Website`, `Age`)" + System.Environment.NewLine +
                "VALUES" + System.Environment.NewLine +
                "('Just''in', 'github.com', 30)" + System.Environment.NewLine +
                "ON DUPLICATE KEY UPDATE" + System.Environment.NewLine +
                "    `Website` = VALUES(`Website`);");
        }

        [Test]
        public void CanUpsertDataWithMultipleMatchColumns()
        {
            var expression = GeneratorTestHelper.GetUpsertDataExpressionWithMultipleKeys();
            
            var result = Generator.Generate(expression);
            result.ShouldBe(
                "INSERT INTO `TestTable1`" + System.Environment.NewLine +
                "(`Name`, `Age`, `Website`)" + System.Environment.NewLine +
                "VALUES" + System.Environment.NewLine +
                "('Just''in', 30, 'github.com')" + System.Environment.NewLine +
                "ON DUPLICATE KEY UPDATE" + System.Environment.NewLine +
                "    `Website` = VALUES(`Website`);");
        }

        [Test]
        public void CanUpsertDataWithRawUpdateValues()
        {
            var expression = GeneratorTestHelper.GetUpsertDataExpressionWithRawUpdateValues();
            
            var result = Generator.Generate(expression);
            result.ShouldBe(
                "INSERT INTO `TestTable1`" + System.Environment.NewLine +
                "(`Name`, `Website`, `Email`)" + System.Environment.NewLine +
                "VALUES" + System.Environment.NewLine +
                "('Just''in', 'github.com', 'test@example.com')" + System.Environment.NewLine +
                "ON DUPLICATE KEY UPDATE" + System.Environment.NewLine +
                "    `Website` = 'codethinked.com'," + System.Environment.NewLine +
                "    `Email` = UPPER('admin@example.com');");
        }

        [Test]
        public void CanUpsertDataWithIgnoreInsertIfExists()
        {
            var expression = GeneratorTestHelper.GetUpsertDataExpression();
            expression.IgnoreInsertIfExists = true;
            
            var result = Generator.Generate(expression);
            result.ShouldBe(
                "INSERT IGNORE INTO `TestTable1`" + System.Environment.NewLine +
                "(`Name`, `Website`)" + System.Environment.NewLine +
                "VALUES" + System.Environment.NewLine +
                "('Just''in', 'github.com');");
        }
    }
}