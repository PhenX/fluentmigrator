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
using FluentMigrator.Builders.Alter;
using FluentMigrator.Builders.Create;
using FluentMigrator.Builders.Delete;
using FluentMigrator.Builders.Execute;
using FluentMigrator.Builders.If;
using FluentMigrator.Builders.Insert;
using FluentMigrator.Builders.Rename;
using FluentMigrator.Builders.Schema;
using FluentMigrator.Builders.Update;
using FluentMigrator.Infrastructure;

namespace FluentMigrator.Builders.If
{
    /// <summary>
    /// Implementation of migration expressions for conditional execution
    /// </summary>
    public class IfThenMigrationExpressionRoot : IIfThenMigrationExpressionRoot
    {
        private readonly IMigrationContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="IfThenMigrationExpressionRoot"/> class
        /// </summary>
        /// <param name="context">The migration context</param>
        public IfThenMigrationExpressionRoot(IMigrationContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc />
        public IAlterExpressionRoot Alter => new AlterExpressionRoot(_context);

        /// <inheritdoc />
        public ICreateExpressionRoot Create => new CreateExpressionRoot(_context);

        /// <inheritdoc />
        public IDeleteExpressionRoot Delete => new DeleteExpressionRoot(_context);

        /// <inheritdoc />
        public IRenameExpressionRoot Rename => new RenameExpressionRoot(_context);

        /// <inheritdoc />
        public IInsertExpressionRoot Insert => new InsertExpressionRoot(_context);

        /// <inheritdoc />
        public IExecuteExpressionRoot Execute => new ExecuteExpressionRoot(_context);

        /// <inheritdoc />
        public ISchemaExpressionRoot Schema => new SchemaExpressionRoot(_context);

        /// <inheritdoc />
        public IUpdateExpressionRoot Update => new UpdateExpressionRoot(_context);
    }
}