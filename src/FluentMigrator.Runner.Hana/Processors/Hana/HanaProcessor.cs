using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using FluentMigrator.Runner.Generators.Hana;
using FluentMigrator.Runner.Initialization;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FluentMigrator.Runner.Processors.Hana
{
    /// <summary>
    /// The SAP Hana processor for FluentMigrator.
    /// </summary>
    public class HanaProcessor : GenericProcessorBase
    {
        /// <inheritdoc />
        public HanaProcessor(
            [NotNull] HanaDbFactory factory,
            [NotNull] HanaGenerator generator,
            [NotNull] HanaQuoter quoter,
            [NotNull] ILogger<HanaProcessor> logger,
            [NotNull] IOptionsSnapshot<ProcessorOptions> options,
            [NotNull] IConnectionStringAccessor connectionStringAccessor)
            : base(() => factory.Factory, generator, quoter, logger, options.Value, connectionStringAccessor)
        {
        }

        /// <inheritdoc />
        public override string DatabaseType => ProcessorIdConstants.Hana;

        /// <inheritdoc />
        protected override string TableExistsQuery =>
            "SELECT 1 FROM TABLES WHERE SCHEMA_NAME = CURRENT_SCHEMA AND upper(TABLE_NAME) = '{1}'";

        /// <inheritdoc />
        protected override string ColumnExistsQuery =>
            "SELECT 1 FROM TABLE_COLUMNS WHERE SCHEMA_NAME = CURRENT_SCHEMA AND upper(TABLE_NAME) = '{1}' AND upper(COLUMN_NAME) = '{2}'";

        /// <inheritdoc />
        protected override string ConstraintExistsQuery =>
            "SELECT 1 FROM CONSTRAINTS WHERE SCHEMA_NAME = CURRENT_SCHEMA and upper(CONSTRAINT_NAME) = '{2}'";

        /// <inheritdoc />
        protected override string IndexExistsQuery =>
            "SELECT 1 FROM INDEXES WHERE SCHEMA_NAME = CURRENT_SCHEMA AND upper(INDEX_NAME) = '{2}'";

        /// <inheritdoc />
        protected override string SequenceExistsQuery =>
            "SELECT 1 FROM SEQUENCES WHERE SCHEMA_NAME = CURRENT_SCHEMA and upper(SEQUENCE_NAME) = '{1}'";

        /// <inheritdoc />
        public override IList<string> DatabaseTypeAliases { get; } = new List<string>();

        /// <inheritdoc />
        protected override string FormatName(string name)
        {
            return base.FormatName(Quoter.UnQuote(name).ToUpper());
        }

        /// <inheritdoc />
        public override bool SchemaExists(string schemaName)
        {
            return false;
        }

        /// <inheritdoc />
        public override bool DefaultValueExists(string schemaName, string tableName, string columnName, object defaultValue)
        {
            return false;
        }

        /// <inheritdoc />
        protected override void Process(string sql)
        {
            Logger.LogSql(sql);

            if (Options.PreviewOnly || string.IsNullOrEmpty(sql))
                return;

            EnsureConnectionIsOpen();

            var batches = Regex.Split(sql, @"^\s*;\s*$", RegexOptions.Multiline)
                .Where(x => !string.IsNullOrEmpty(x))
                .Select(c => c.Trim());

            foreach (var batch in batches)
            {
                var batchCommand = batch.EndsWith(";")
                    ? batch.Remove(batch.Length - 1)
                    : batch;

                using (var command = CreateCommand(batchCommand))
                    command.ExecuteNonQuery();
            }
        }
    }
}
