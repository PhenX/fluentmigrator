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
using System.Linq.Expressions;
using FluentMigrator.Builders.If;
using FluentMigrator.Builders.Schema;
using FluentMigrator.Expressions;
using FluentMigrator.Infrastructure;

namespace FluentMigrator.Builders.If
{
    /// <summary>
    /// Implementation of conditional schema-based expressions
    /// </summary>
    public class IfExpressionRoot : IIfExpressionRoot
    {
        private readonly IMigrationContext _context;
        private readonly Expression<Func<ISchemaExpressionRoot, bool>> _condition;

        /// <summary>
        /// Initializes a new instance of the <see cref="IfExpressionRoot"/> class
        /// </summary>
        /// <param name="context">The migration context</param>
        /// <param name="condition">The condition expression to evaluate</param>
        public IfExpressionRoot(IMigrationContext context, Expression<Func<ISchemaExpressionRoot, bool>> condition)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        /// <inheritdoc />
        public IFluentSyntax Then(Action<IIfThenMigrationExpressionRoot> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            // Create a conditional expression that wraps the actions
            var conditionalExpression = new FluentMigrator.Expressions.ConditionalExpression
            {
                Condition = _condition,
                Actions = new List<IMigrationExpression>()
            };

            // Add to current context
            _context.Expressions.Add(conditionalExpression);

            // Create a context that will collect expressions for conditional execution
            var conditionalContext = new ConditionalMigrationContext(_context, conditionalExpression.Actions);
            var thenRoot = new IfThenMigrationExpressionRoot(conditionalContext);
            
            // Execute the action to collect the expressions
            action(thenRoot);

            return this;
        }
    }
}