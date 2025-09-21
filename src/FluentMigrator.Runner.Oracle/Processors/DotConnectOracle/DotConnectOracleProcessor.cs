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

using FluentMigrator.Runner.Generators.Oracle;
using FluentMigrator.Runner.Initialization;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FluentMigrator.Runner.Processors.DotConnectOracle
{
    /// <summary>
    /// The DotConnect Oracle migration processor.
    /// </summary>
    public class DotConnectOracleProcessor : GenericProcessorBase
    {
        /// <inheritdoc />
        public override string DatabaseType => "DotConnectOracle";

        /// <inheritdoc />
        public override IList<string> DatabaseTypeAliases { get; } = new List<string>();

        /// <inheritdoc />
        public DotConnectOracleProcessor(
            [NotNull] DotConnectOracleDbFactory factory,
            [NotNull] IOracleGenerator generator,
            [NotNull] ILogger<DotConnectOracleProcessor> logger,
            [NotNull] IOptionsSnapshot<ProcessorOptions> options,
            [NotNull] IConnectionStringAccessor connectionStringAccessor)
            : base(() => factory.Factory, generator, ((OracleGenerator) generator).Quoter, logger, options.Value, connectionStringAccessor)
        {
        }

        /// <inheritdoc />
        protected override string SchemaExistsQuery => "SELECT 1 FROM ALL_USERS WHERE upper(USERNAME) = '{0}'";

        /// <inheritdoc />
        protected override string TableExistsQuery =>
            "SELECT 1 FROM ALL_TABLES WHERE upper(OWNER) = '{0}' AND upper(TABLE_NAME) = '{1}'";

        /// <inheritdoc />
        protected override string TableWithoutSchemaExistsQuery =>
            "SELECT 1 FROM USER_TABLES WHERE upper(TABLE_NAME) = '{1}'";

        /// <inheritdoc />
        protected override string ColumnExistsQuery =>
            "SELECT 1 FROM ALL_TAB_COLUMNS WHERE upper(OWNER) = '{0}' AND upper(TABLE_NAME) = '{1}' AND upper(COLUMN_NAME) = '{2}'";

        /// <inheritdoc />
        protected override string ColumnWithoutSchemaExistsQuery =>
            "SELECT 1 FROM USER_TAB_COLUMNS WHERE upper(TABLE_NAME) = '{1}' AND upper(COLUMN_NAME) = '{2}'";

        /// <inheritdoc />
        protected override string ConstraintExistsQuery =>
            "SELECT 1 FROM ALL_CONSTRAINTS WHERE upper(OWNER) = '{0}' AND upper(CONSTRAINT_NAME) = '{2}'";

        /// <inheritdoc />
        protected override string ConstraintWithoutSchemaExistsQuery =>
            "SELECT 1 FROM USER_CONSTRAINTS WHERE upper(CONSTRAINT_NAME) = '{2}'";

        /// <inheritdoc />
        protected override string IndexExistsQuery =>
            "SELECT 1 FROM ALL_INDEXES WHERE upper(OWNER) = '{0}' AND upper(INDEX_NAME) = '{2}'";

        /// <inheritdoc />
        protected override string IndexWithoutSchemaExistsQuery =>
            "SELECT 1 FROM USER_INDEXES WHERE upper(INDEX_NAME) = '{2}'";

        /// <inheritdoc />
        protected override string SequenceExistsQuery =>
            "SELECT 1 FROM ALL_SEQUENCES WHERE upper(SEQUENCE_OWNER) = '{0}' AND upper(SEQUENCE_NAME) = '{2}'";

        /// <inheritdoc />
        protected override string SequenceWithoutSchemaExistsQuery =>
            "SELECT 1 FROM USER_SEQUENCES WHERE upper(SEQUENCE_NAME) = '{2}'";

        /// <inheritdoc />
        protected override string DefaultValueExistsQuery =>
            "SELECT 1 FROM ALL_TAB_COLUMNS WHERE upper(OWNER) = '{0}' AND upper(TABLE_NAME) = '{1}' AND upper(COLUMN_NAME) = '{2}' AND DATA_DEFAULT IS NOT NULL";

        /// <inheritdoc />
        protected override string DefaultValueWithoutSchemaExistsQuery =>
            "SELECT 1 FROM USER_TAB_COLUMNS WHERE upper(TABLE_NAME) = '{1}' AND upper(COLUMN_NAME) = '{2}' AND DATA_DEFAULT IS NOT NULL";

        /// <inheritdoc />
        protected override string FormatSchemaName(string schemaName)
        {
            return base.FormatSchemaName(schemaName)?.ToUpper();
        }

        /// <inheritdoc />
        protected override string FormatName(string name)
        {
            return base.FormatName(name)?.ToUpper();
        }
    }
}
