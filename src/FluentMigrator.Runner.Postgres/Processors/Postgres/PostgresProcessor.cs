#region License
//
// Copyright (c) 2018, Fluent Migrator Project
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

using FluentMigrator.Runner.Generators.Postgres;
using FluentMigrator.Runner.Helpers;
using FluentMigrator.Runner.Initialization;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FluentMigrator.Runner.Processors.Postgres
{
    /// <summary>
    /// The PostgreSQL migration processor.
    /// </summary>
    public class PostgresProcessor : GenericProcessorBase
    {
        private PostgresQuoter PgQuoter => (PostgresQuoter)Quoter;

        /// <inheritdoc />
        public override string DatabaseType => ProcessorIdConstants.Postgres;

        /// <inheritdoc />
        public override IList<string> DatabaseTypeAliases { get; } = new List<string> { ProcessorIdConstants.PostgreSQL };

        /// <inheritdoc />
        public PostgresProcessor(
            [NotNull] PostgresDbFactory factory,
            [NotNull] PostgresGenerator generator,
            [NotNull] ILogger<PostgresProcessor> logger,
            [NotNull] IOptionsSnapshot<ProcessorOptions> options,
            [NotNull] IConnectionStringAccessor connectionStringAccessor,
            [NotNull] PostgresOptions pgOptions)
            : base(() => factory.Factory, generator, new PostgresQuoter(pgOptions), logger, options.Value, connectionStringAccessor)
        {
            if (pgOptions == null)
            {
                throw new ArgumentNullException(nameof(pgOptions));
            }
        }

        /// <inheritdoc />
        protected override string SchemaExistsQuery =>
            "select * from information_schema.schemata where schema_name = '{0}'";

        /// <inheritdoc />
        protected override string TableExistsQuery =>
            "select * from information_schema.tables where table_schema = '{0}' and table_name = '{1}'";

        /// <inheritdoc />
        protected override string ColumnExistsQuery =>
            "select * from information_schema.columns where table_schema = '{0}' and table_name = '{1}' and column_name = '{2}'";

        /// <inheritdoc />
        protected override string ConstraintExistsQuery =>
            "select * from information_schema.table_constraints where constraint_catalog = current_catalog and table_schema = '{0}' and table_name = '{1}' and constraint_name = '{2}'";

        /// <inheritdoc />
        protected override string IndexExistsQuery =>
            "select * from pg_catalog.pg_indexes where schemaname='{0}' and tablename = '{1}' and indexname = '{2}'";

        /// <inheritdoc />
        protected override string SequenceExistsQuery =>
            "select * from information_schema.sequences where sequence_catalog = current_catalog and sequence_schema ='{0}' and sequence_name = '{1}'";

        /// <inheritdoc />
        protected override string DefaultValueExistsQuery =>
            "select * from information_schema.columns where table_schema = '{0}' and table_name = '{1}' and column_name = '{2}' and column_default like '{3}'";

        /// <inheritdoc />
        protected override string FormatSchemaName(string schemaName)
        {
            var schemaNameCased = schemaName;
            if (!PgQuoter.Options.ForceQuote)
            {
                schemaNameCased = schemaName?.ToLowerInvariant();
            }

            return FormatHelper.FormatSqlEscape(PgQuoter.UnQuoteSchemaName(schemaNameCased));
        }

        /// <inheritdoc />
        protected override string FormatName(string name)
        {
            var sqlNameCased = name;
            if (!PgQuoter.Options.ForceQuote)
            {
                sqlNameCased = name?.ToLowerInvariant();
            }

            return FormatHelper.FormatSqlEscape(Quoter.UnQuote(sqlNameCased));
        }
    }
}
