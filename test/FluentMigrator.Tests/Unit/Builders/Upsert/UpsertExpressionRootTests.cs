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
using FluentMigrator.Infrastructure;
using NUnit.Framework;
using Shouldly;

namespace FluentMigrator.Tests.Unit.Builders.Upsert
{
    [TestFixture]
    [Category("Builder")]
    [Category("UpsertData")]
    public class UpsertExpressionRootTests
    {
        private IMigrationContext _context;

        [SetUp]
        public void Setup()
        {
            _context = new MockMigrationContext();
        }

        [Test]
        public void ShouldCreateUpsertDataExpressionWhenIntoTableIsCalled()
        {
            var root = new UpsertExpressionRoot(_context);

            root.IntoTable("TestTable");

            _context.Expressions.Count.ShouldBe(1);
            var expression = _context.Expressions.First() as UpsertDataExpression;
            expression.ShouldNotBeNull();
            expression.TableName.ShouldBe("TestTable");
        }

        [Test]
        public void ShouldReturnUpsertDataExpressionBuilderWhenIntoTableIsCalled()
        {
            var root = new UpsertExpressionRoot(_context);

            var builder = root.IntoTable("TestTable");

            builder.ShouldNotBeNull();
            builder.ShouldBeOfType<UpsertDataExpressionBuilder>();
        }

        [Test]
        public void ShouldSupportFluentChaining()
        {
            var root = new UpsertExpressionRoot(_context);

            root.IntoTable("Users")
                .MatchOn("Email")
                .Row(new { Email = "test@example.com", Name = "Test User" });

            _context.Expressions.Count.ShouldBe(1);
            var expression = _context.Expressions.First() as UpsertDataExpression;
            expression.ShouldNotBeNull();
            expression.TableName.ShouldBe("Users");
            expression.MatchColumns.ShouldContain("Email");
            expression.Rows.Count.ShouldBe(1);
        }
    }

    public class MockMigrationContext : IMigrationContext
    {
        public System.Collections.Generic.ICollection<IMigrationExpression> Expressions { get; set; } = 
            new System.Collections.Generic.List<IMigrationExpression>();
        
        public string Connection { get; set; } = "test-connection";
        public object ApplicationContext { get; set; }
        public IQuerySchema QuerySchema { get; set; } = null;
        public System.IServiceProvider ServiceProvider { get; set; } = null;
    }
}