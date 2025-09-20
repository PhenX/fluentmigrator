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
using FluentMigrator.Expressions;
using FluentMigrator.Infrastructure;

namespace FluentMigrator.Builders.If
{
    /// <summary>
    /// Migration context that collects expressions for conditional execution
    /// </summary>
    internal class ConditionalMigrationContext : IMigrationContext
    {
        private readonly IMigrationContext _parentContext;
        private readonly IList<IMigrationExpression> _targetExpressions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConditionalMigrationContext"/> class
        /// </summary>
        /// <param name="parentContext">The parent migration context</param>
        /// <param name="targetExpressions">The list to add expressions to</param>
        public ConditionalMigrationContext(IMigrationContext parentContext, IList<IMigrationExpression> targetExpressions)
        {
            _parentContext = parentContext ?? throw new ArgumentNullException(nameof(parentContext));
            _targetExpressions = targetExpressions ?? throw new ArgumentNullException(nameof(targetExpressions));
            Connection = parentContext.Connection;
            Expressions = (ICollection<IMigrationExpression>)targetExpressions;
        }

        /// <inheritdoc />
        public string Connection { get; set; }

        /// <inheritdoc />
        public ICollection<IMigrationExpression> Expressions { get; set; }

        /// <inheritdoc />
        public IQuerySchema QuerySchema => _parentContext.QuerySchema;

        /// <inheritdoc />
        public IServiceProvider ServiceProvider => _parentContext.ServiceProvider;
    }
}