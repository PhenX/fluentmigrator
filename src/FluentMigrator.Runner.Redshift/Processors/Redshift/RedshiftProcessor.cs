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

using System.Collections.Generic;

using FluentMigrator.Runner.Generators.Redshift;
using FluentMigrator.Runner.Helpers;
using FluentMigrator.Runner.Initialization;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FluentMigrator.Runner.Processors.Redshift
{
    /// <summary>
    /// The Amazon Redshift processor for FluentMigrator.
    /// </summary>
    public class RedshiftProcessor : GenericProcessorBase
    {
        /// <inheritdoc />
        public override string DatabaseType => ProcessorIdConstants.Redshift;

        /// <inheritdoc />
        public override IList<string> DatabaseTypeAliases { get; } = new List<string>();

        /// <inheritdoc />
        public RedshiftProcessor(
            [NotNull] RedshiftDbFactory factory,
            [NotNull] RedshiftGenerator generator,
            [NotNull] RedshiftQuoter quoter,
            [NotNull] ILogger<RedshiftProcessor> logger,
            [NotNull] IOptionsSnapshot<ProcessorOptions> options,
            [NotNull] IConnectionStringAccessor connectionStringAccessor)
            : base(() => factory.Factory, generator, quoter, logger, options.Value, connectionStringAccessor)
        {
        }

        /// <inheritdoc />
        protected override string SchemaExistsQuery =>
            "select * from information_schema.schemata where schema_name ilike '{0}'";

        /// <inheritdoc />
        protected override string TableExistsQuery =>
            "select * from information_schema.tables where table_schema ilike '{0}' and table_name ilike '{1}'";

        /// <inheritdoc />
        protected override string ColumnExistsQuery =>
            "select * from information_schema.columns where table_schema ilike '{0}' and table_name ilike '{1}' and column_name ilike '{2}'";

        /// <inheritdoc />
        protected override string DefaultValueExistsQuery =>
            "select * from information_schema.columns where table_schema ilike '{0}' and table_name ilike '{1}' and column_name ilike '{2}' and column_default like '{3}'";

        /// <inheritdoc />
        protected override string FormatSchemaName(string schemaName)
        {
            return FormatHelper.FormatSqlEscape(((RedshiftQuoter)Quoter).UnQuoteSchemaName(schemaName));
        }

        /// <inheritdoc />
        protected override string FormatName(string name)
        {
            return FormatHelper.FormatSqlEscape(Quoter.UnQuote(name));
        }

        /// <inheritdoc />
        public override bool ConstraintExists(string schemaName, string tableName, string constraintName)
        {
            return false;
        }

        /// <inheritdoc />
        public override bool IndexExists(string schemaName, string tableName, string indexName)
        {
            return false;
        }

        /// <inheritdoc />
        public override bool SequenceExists(string schemaName, string sequenceName)
        {
            return false;
        }
    }
}
