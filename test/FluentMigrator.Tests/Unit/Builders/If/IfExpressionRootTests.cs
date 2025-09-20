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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using FluentMigrator.Builders.If;
using FluentMigrator.Builders.Schema;
using FluentMigrator.Expressions;
using FluentMigrator.Infrastructure;
using FluentMigrator.Runner;

using Microsoft.Extensions.DependencyInjection;

using Moq;

using NUnit.Framework;

using Shouldly;

namespace FluentMigrator.Tests.Unit.Builders.If
{
    [TestFixture]
    [Category("Builder")]
    [Category("Conditional")]
    public class IfExpressionRootTests
    {
        [Test]
        public void CreatesConditionalExpressionWhenThenCalled()
        {
            // Arrange
            var context = CreateMockContext();
            Expression<Func<ISchemaExpressionRoot, bool>> condition = _ => _.Table("TestTable").Exists();
            var ifRoot = new IfExpressionRoot(context, condition);

            // Act
            ifRoot.Then(_ => _.Create.Table("NewTable").WithColumn("Id").AsInt32());

            // Assert
            context.Expressions.Count.ShouldBe(1);
            context.Expressions.First().ShouldBeOfType<ConditionalExpression>();
            
            var conditionalExpr = (ConditionalExpression)context.Expressions.First();
            conditionalExpr.Condition.ShouldBe(condition);
            conditionalExpr.Actions.Count.ShouldBe(1);
        }

        [Test]
        public void ExecutesActionsWhenConditionIsTrue()
        {
            // Arrange
            var mockProcessor = new Mock<IMigrationProcessor>();
            mockProcessor.Setup(x => x.TableExists(null, "TestTable")).Returns(true);
            
            var conditionalExpression = new ConditionalExpression
            {
                Condition = _ => _.Table("TestTable").Exists(),
                Actions = new List<IMigrationExpression>()
            };

            var mockAction = new Mock<IMigrationExpression>();
            conditionalExpression.Actions.Add(mockAction.Object);

            // Act
            conditionalExpression.ExecuteWith(mockProcessor.Object);

            // Assert
            mockAction.Verify(x => x.ExecuteWith(mockProcessor.Object), Times.Once);
        }

        [Test]
        public void DoesNotExecuteActionsWhenConditionIsFalse()
        {
            // Arrange
            var mockProcessor = new Mock<IMigrationProcessor>();
            mockProcessor.Setup(x => x.TableExists(null, "TestTable")).Returns(false);
            
            var conditionalExpression = new ConditionalExpression
            {
                Condition = _ => _.Table("TestTable").Exists(),
                Actions = new List<IMigrationExpression>()
            };

            var mockAction = new Mock<IMigrationExpression>();
            conditionalExpression.Actions.Add(mockAction.Object);

            // Act
            conditionalExpression.ExecuteWith(mockProcessor.Object);

            // Assert
            mockAction.Verify(x => x.ExecuteWith(mockProcessor.Object), Times.Never);
        }

        [Test]
        public void SupportsOrConditions()
        {
            // Arrange
            var mockProcessor = new Mock<IMigrationProcessor>();
            mockProcessor.Setup(x => x.TableExists(null, "TestTable")).Returns(false);
            mockProcessor.Setup(x => x.SchemaExists("TestSchema")).Returns(true);
            
            var conditionalExpression = new ConditionalExpression
            {
                Condition = _ => _.Table("TestTable").Exists() || _.Schema("TestSchema").Exists(),
                Actions = new List<IMigrationExpression>()
            };

            var mockAction = new Mock<IMigrationExpression>();
            conditionalExpression.Actions.Add(mockAction.Object);

            // Act
            conditionalExpression.ExecuteWith(mockProcessor.Object);

            // Assert - Should execute because second condition is true
            mockAction.Verify(x => x.ExecuteWith(mockProcessor.Object), Times.Once);
        }

        [Test]
        public void SupportsAndConditions()
        {
            // Arrange
            var mockProcessor = new Mock<IMigrationProcessor>();
            mockProcessor.Setup(x => x.TableExists(null, "TestTable")).Returns(true);
            mockProcessor.Setup(x => x.SchemaExists("TestSchema")).Returns(false);
            
            var conditionalExpression = new ConditionalExpression
            {
                Condition = _ => _.Table("TestTable").Exists() && _.Schema("TestSchema").Exists(),
                Actions = new List<IMigrationExpression>()
            };

            var mockAction = new Mock<IMigrationExpression>();
            conditionalExpression.Actions.Add(mockAction.Object);

            // Act
            conditionalExpression.ExecuteWith(mockProcessor.Object);

            // Assert - Should not execute because second condition is false
            mockAction.Verify(x => x.ExecuteWith(mockProcessor.Object), Times.Never);
        }

        [Test]
        public void HandlesExceptionInConditionEvaluation()
        {
            // Arrange
            var mockProcessor = new Mock<IMigrationProcessor>();
            mockProcessor.Setup(x => x.TableExists(null, "TestTable")).Throws(new Exception("Database error"));
            
            var conditionalExpression = new ConditionalExpression
            {
                Condition = _ => _.Table("TestTable").Exists(),
                Actions = new List<IMigrationExpression>()
            };

            var mockAction = new Mock<IMigrationExpression>();
            conditionalExpression.Actions.Add(mockAction.Object);

            // Act & Assert - Should not throw and should not execute actions
            Should.NotThrow(() => conditionalExpression.ExecuteWith(mockProcessor.Object));
            mockAction.Verify(x => x.ExecuteWith(mockProcessor.Object), Times.Never);
        }

        private IMigrationContext CreateMockContext()
        {
            var mock = new Mock<IMigrationContext>();
            var expressions = new List<IMigrationExpression>();
            mock.Setup(x => x.Expressions).Returns(expressions);
            mock.Setup(x => x.QuerySchema).Returns(Mock.Of<IQuerySchema>());
            return mock.Object;
        }
    }
}