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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using FluentMigrator.Expressions;
using FluentMigrator.Model;
using NUnit.Framework;
using Shouldly;

namespace FluentMigrator.Tests.Unit.Expressions
{
    [TestFixture]
    [Category("Expression")]
    [Category("UpsertData")]
    public class UpsertDataExpressionTests
    {
        private UpsertDataExpression _expression;

        [SetUp]
        public void Initialize()
        {
            _expression = new UpsertDataExpression
            {
                TableName = "Users",
                SchemaName = "dbo"
            };
            _expression.MatchColumns.Add("Email");

            var row = new InsertionDataDefinition();
            row.Add(new KeyValuePair<string, object>("Email", "test@example.com"));
            row.Add(new KeyValuePair<string, object>("Name", "Test User"));
            row.Add(new KeyValuePair<string, object>("IsActive", true));
            _expression.Rows.Add(row);
        }

        [Test]
        public void ShouldHaveCorrectTableName()
        {
            _expression.TableName.ShouldBe("Users");
        }

        [Test]
        public void ShouldHaveCorrectSchemaName()
        {
            _expression.SchemaName.ShouldBe("dbo");
        }

        [Test]
        public void ShouldHaveMatchColumns()
        {
            _expression.MatchColumns.ShouldContain("Email");
            _expression.MatchColumns.Count.ShouldBe(1);
        }

        [Test]
        public void ShouldHaveRowData()
        {
            _expression.Rows.Count.ShouldBe(1);
            var row = _expression.Rows.First();
            row.Any(kvp => kvp.Key == "Email").ShouldBeTrue();
            row.Any(kvp => kvp.Key == "Name").ShouldBeTrue();
            row.Any(kvp => kvp.Key == "IsActive").ShouldBeTrue();
        }

        [Test]
        public void ValidationShouldPassForValidExpression()
        {
            var context = new ValidationContext(_expression);
            var results = _expression.Validate(context);
            results.ShouldBeEmpty();
        }

        [Test]
        public void ValidationShouldFailWhenNoMatchColumns()
        {
            _expression.MatchColumns.Clear();
            
            var context = new ValidationContext(_expression);
            var results = _expression.Validate(context).ToList();
            
            results.ShouldNotBeEmpty();
            results.ShouldContain(r => r.ErrorMessage.Contains("must specify at least one match column"));
        }

        [Test]
        public void ValidationShouldFailWhenNoRows()
        {
            _expression.Rows.Clear();
            
            var context = new ValidationContext(_expression);
            var results = _expression.Validate(context).ToList();
            
            results.ShouldNotBeEmpty();
            results.ShouldContain(r => r.ErrorMessage.Contains("must specify at least one row"));
        }

        [Test]
        public void ValidationShouldFailWhenRowMissingMatchColumn()
        {
            var row = new InsertionDataDefinition();
            row.Add(new KeyValuePair<string, object>("Name", "Test User")); // Missing Email
            _expression.Rows.Clear();
            _expression.Rows.Add(row);
            
            var context = new ValidationContext(_expression);
            var results = _expression.Validate(context).ToList();
            
            results.ShouldNotBeEmpty();
            results.ShouldContain(r => r.ErrorMessage.Contains("Missing column: Email"));
        }

        [Test]
        public void ValidationShouldFailWhenUpdateColumnsIncludeMatchColumns()
        {
            _expression.UpdateColumns = new List<string> { "Email", "Name" }; // Email is match column
            
            var context = new ValidationContext(_expression);
            var results = _expression.Validate(context).ToList();
            
            results.ShouldNotBeEmpty();
            results.ShouldContain(r => r.ErrorMessage.Contains("Update columns cannot include match columns"));
        }

        [Test]
        public void ShouldCreateCorrectReverseExpression()
        {
            var reverse = _expression.Reverse() as DeleteDataExpression;
            
            reverse.ShouldNotBeNull();
            reverse.TableName.ShouldBe("Users");
            reverse.SchemaName.ShouldBe("dbo");
            reverse.Rows.Count.ShouldBe(1);
            
            var deleteRow = reverse.Rows.First();
            deleteRow.Any(kvp => kvp.Key == "Email").ShouldBeTrue();
            deleteRow.Count.ShouldBe(1); // Should only contain match columns
        }

        [Test]
        public void ShouldCreateReverseExpressionWithMultipleMatchColumns()
        {
            _expression.MatchColumns.Add("Category");
            var row = _expression.Rows.First();
            row.Add(new KeyValuePair<string, object>("Category", "Electronics"));

            var reverse = _expression.Reverse() as DeleteDataExpression;
            
            reverse.ShouldNotBeNull();
            var deleteRow = reverse.Rows.First();
            deleteRow.Any(kvp => kvp.Key == "Email").ShouldBeTrue();
            deleteRow.Any(kvp => kvp.Key == "Category").ShouldBeTrue();
            deleteRow.Count.ShouldBe(2); // Should contain both match columns
        }

        [Test]
        public void ShouldSupportMultipleRows()
        {
            var row2 = new InsertionDataDefinition();
            row2.Add(new KeyValuePair<string, object>("Email", "test2@example.com"));
            row2.Add(new KeyValuePair<string, object>("Name", "Test User 2"));
            row2.Add(new KeyValuePair<string, object>("IsActive", false));
            _expression.Rows.Add(row2);

            _expression.Rows.Count.ShouldBe(2);
            
            var context = new ValidationContext(_expression);
            var results = _expression.Validate(context);
            results.ShouldBeEmpty();
        }

        [Test]
        public void ShouldSupportUpdateColumnsSpecification()
        {
            _expression.UpdateColumns = new List<string> { "Name", "IsActive" };
            
            _expression.UpdateColumns.ShouldContain("Name");
            _expression.UpdateColumns.ShouldContain("IsActive");
            _expression.UpdateColumns.ShouldNotContain("Email");
        }
    }
}