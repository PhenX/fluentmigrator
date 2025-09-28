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

using System.Linq;
using FluentMigrator.Builders.Upsert;
using FluentMigrator.Expressions;
using NUnit.Framework;
using Shouldly;

namespace FluentMigrator.Tests.Unit.Builders.Upsert
{
    [TestFixture]
    [Category("Builder")]
    [Category("UpsertData")]
    public class UpsertDataExpressionBuilderTests
    {
        [Test]
        public void ShouldSetSchemaNameWhenInSchemaIsCalled()
        {
            var expression = new UpsertDataExpression();
            var builder = new UpsertDataExpressionBuilder(expression);

            builder.InSchema("TestSchema");

            expression.SchemaName.ShouldBe("TestSchema");
        }

        [Test]
        public void ShouldSetMatchColumnsWhenMatchOnIsCalled()
        {
            var expression = new UpsertDataExpression();
            var builder = new UpsertDataExpressionBuilder(expression);

            builder.MatchOn("Email", "Category");

            expression.MatchColumns.ShouldContain("Email");
            expression.MatchColumns.ShouldContain("Category");
            expression.MatchColumns.Count.ShouldBe(2);
        }

        [Test]
        public void ShouldAddRowWhenRowIsCalled()
        {
            var expression = new UpsertDataExpression();
            var builder = new UpsertDataExpressionBuilder(expression);

            builder.Row(new { Email = "test@example.com", Name = "Test User", IsActive = true });

            expression.Rows.Count.ShouldBe(1);
            var row = expression.Rows.First();
            row.Any(kvp => kvp.Key == "Email" && kvp.Value.Equals("test@example.com")).ShouldBeTrue();
            row.Any(kvp => kvp.Key == "Name" && kvp.Value.Equals("Test User")).ShouldBeTrue();
            row.Any(kvp => kvp.Key == "IsActive" && kvp.Value.Equals(true)).ShouldBeTrue();
        }

        [Test]
        public void ShouldAddMultipleRowsWhenRowsIsCalled()
        {
            var expression = new UpsertDataExpression();
            var builder = new UpsertDataExpressionBuilder(expression);

            builder.Rows(
                new { Email = "test1@example.com", Name = "Test User 1" },
                new { Email = "test2@example.com", Name = "Test User 2" },
                new { Email = "test3@example.com", Name = "Test User 3" }
            );

            expression.Rows.Count.ShouldBe(3);
            
            expression.Rows[0].Any(kvp => kvp.Key == "Email" && kvp.Value.Equals("test1@example.com")).ShouldBeTrue();
            expression.Rows[1].Any(kvp => kvp.Key == "Email" && kvp.Value.Equals("test2@example.com")).ShouldBeTrue();
            expression.Rows[2].Any(kvp => kvp.Key == "Email" && kvp.Value.Equals("test3@example.com")).ShouldBeTrue();
        }

        [Test]
        public void ShouldSetUpdateColumnsWhenUpdateColumnsIsCalled()
        {
            var expression = new UpsertDataExpression();
            var builder = new UpsertDataExpressionBuilder(expression);

            builder.UpdateColumns("Name", "IsActive");

            expression.UpdateColumns.ShouldContain("Name");
            expression.UpdateColumns.ShouldContain("IsActive");
            expression.UpdateColumns.Count.ShouldBe(2);
        }

        [Test]
        public void ShouldSupportFluentChaining()
        {
            var expression = new UpsertDataExpression();
            var builder = new UpsertDataExpressionBuilder(expression);

            builder
                .InSchema("TestSchema")
                .MatchOn("Email")
                .Row(new { Email = "test@example.com", Name = "Test User", IsActive = true })
                .UpdateColumns("Name", "IsActive");

            expression.SchemaName.ShouldBe("TestSchema");
            expression.MatchColumns.ShouldContain("Email");
            expression.Rows.Count.ShouldBe(1);
            expression.UpdateColumns.ShouldContain("Name");
            expression.UpdateColumns.ShouldContain("IsActive");
        }

        [Test]
        public void ShouldHandleComplexDataTypes()
        {
            var expression = new UpsertDataExpression();
            var builder = new UpsertDataExpressionBuilder(expression);

            builder.Row(new 
            { 
                Id = 123,
                Name = "Test User",
                Email = "test@example.com",
                IsActive = true,
                CreatedDate = System.DateTime.Parse("2024-01-01"),
                Amount = 99.99m,
                Tags = new[] { "tag1", "tag2" }.ToString()
            });

            expression.Rows.Count.ShouldBe(1);
            var row = expression.Rows.First();
            
            row.Any(kvp => kvp.Key == "Id" && kvp.Value.Equals(123)).ShouldBeTrue();
            row.Any(kvp => kvp.Key == "Name" && kvp.Value.Equals("Test User")).ShouldBeTrue();
            row.Any(kvp => kvp.Key == "IsActive" && kvp.Value.Equals(true)).ShouldBeTrue();
            row.Any(kvp => kvp.Key == "Amount" && kvp.Value.Equals(99.99m)).ShouldBeTrue();
        }

        [Test]
        public void ShouldClearPreviousMatchColumnsWhenMatchOnIsCalled()
        {
            var expression = new UpsertDataExpression();
            expression.MatchColumns.Add("OldColumn");
            var builder = new UpsertDataExpressionBuilder(expression);

            builder.MatchOn("Email", "Category");

            expression.MatchColumns.ShouldNotContain("OldColumn");
            expression.MatchColumns.ShouldContain("Email");
            expression.MatchColumns.ShouldContain("Category");
            expression.MatchColumns.Count.ShouldBe(2);
        }

        [Test]
        public void ShouldSetIgnoreInsertIfExistsWhenIgnoreInsertIfExistsIsCalled()
        {
            var expression = new UpsertDataExpression();
            var builder = new UpsertDataExpressionBuilder(expression);

            builder.IgnoreInsertIfExists();

            expression.IgnoreInsertIfExists.ShouldBeTrue();
        }

        [Test]
        public void ShouldSupportIgnoreInsertIfExistsWithFluentChaining()
        {
            var expression = new UpsertDataExpression();
            var builder = new UpsertDataExpressionBuilder(expression);

            builder
                .InSchema("TestSchema")
                .MatchOn("Email")
                .Row(new { Email = "test@example.com", Name = "Test User", IsActive = true })
                .IgnoreInsertIfExists();

            expression.SchemaName.ShouldBe("TestSchema");
            expression.MatchColumns.ShouldContain("Email");
            expression.Rows.Count.ShouldBe(1);
            expression.IgnoreInsertIfExists.ShouldBeTrue();
        }
    }
}