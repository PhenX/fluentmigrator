#region License

// Copyright (c) 2007-2024, Fluent Migrator Project
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

using FluentMigrator.Expressions;
using FluentMigrator.Generation;
using FluentMigrator.Runner.Helpers;
using FluentMigrator.Runner.Initialization;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

namespace FluentMigrator.Runner.Processors
{
    /// <summary>
    /// Base class for generic database processors in FluentMigrator.
    /// </summary>
    public abstract class GenericProcessorBase : ProcessorBase
    {
        [NotNull, ItemCanBeNull]
        private readonly Lazy<DbProviderFactory> _dbProviderFactory;

        [NotNull, ItemCanBeNull]
        private readonly Lazy<IDbConnection> _lazyConnection;

        [CanBeNull]
        private IDbConnection _connection;

        private bool _disposed;

        /// <inheritdoc />
        protected GenericProcessorBase(
            [NotNull] Func<DbProviderFactory> factoryAccessor,
            [NotNull] IMigrationGenerator generator,
            [NotNull] IQuoter quoter,
            [NotNull] ILogger logger,
            [NotNull] ProcessorOptions options,
            [NotNull] IConnectionStringAccessor connectionStringAccessor)
            : base(generator, logger, options)
        {
            _dbProviderFactory = new Lazy<DbProviderFactory>(factoryAccessor.Invoke);

            Quoter = quoter;

            var connectionString = connectionStringAccessor.ConnectionString;

            _lazyConnection = new Lazy<IDbConnection>(
                () =>
                {
                    if (DbProviderFactory == null)
                        return null;
                    var connection = DbProviderFactory.CreateConnection();
                    Debug.Assert(connection != null, nameof(Connection) + " != null");
                    connection!.ConnectionString = connectionString;
                    connection.Open();
                    return connection;
                });
        }

        /// <summary>
        /// Gets the database connection.
        /// </summary>
        public IDbConnection Connection
        {
            get => _connection ?? _lazyConnection.Value;
            protected set => _connection = value;
        }

        /// <summary>
        /// Gets the current database transaction.
        /// </summary>
        [CanBeNull]
        public IDbTransaction Transaction { get; protected set; }

        /// <summary>
        /// Gets the database provider factory.
        /// </summary>
        [CanBeNull]
        protected DbProviderFactory DbProviderFactory => _dbProviderFactory.Value;

        /// <summary>
        /// Gets the quoter used to escape database object names.
        /// </summary>
        [NotNull]
        public IQuoter Quoter { get; }

        /// <summary>
        /// Gets the query to check if a schema exists.
        /// </summary>
        protected virtual string SchemaExistsQuery => null;

        /// <summary>
        /// Gets the query to check if a table exists.
        /// </summary>
        protected virtual string TableExistsQuery => null;

        /// <summary>
        /// Gets the query to check if a column exists.
        /// </summary>
        protected virtual string ColumnExistsQuery => null;

        /// <summary>
        /// Gets the query to check if a constraint exists.
        /// </summary>
        protected virtual string ConstraintExistsQuery => null;

        /// <summary>
        /// Gets the query to check if an index exists.
        /// </summary>
        protected virtual string IndexExistsQuery => null;

        /// <summary>
        /// Gets the query to check if a sequence exists.
        /// </summary>
        protected virtual string SequenceExistsQuery => null;

        /// <summary>
        /// Gets the query to check if a default value exists.
        /// </summary>
        protected virtual string DefaultValueExistsQuery => null;

        /// <summary>
        /// Gets the query to read all data from a table.
        /// </summary>
        protected virtual string ReadTableDataQuery => "SELECT * FROM {0}";

        /// <summary>
        /// Ensures the database connection is open.
        /// </summary>
        protected virtual void EnsureConnectionIsOpen()
        {
            if (Connection != null && Connection.State != ConnectionState.Open)
            {
                Connection.Open();
            }
        }

        /// <summary>
        /// Ensures the database connection is closed.
        /// </summary>
        protected virtual void EnsureConnectionIsClosed()
        {
            if ((_connection != null || (_lazyConnection.IsValueCreated && Connection != null)) && Connection.State != ConnectionState.Closed)
            {
                Connection.Close();
            }
        }

        /// <inheritdoc />
        public override void BeginTransaction()
        {
            if (Transaction != null) return;

            EnsureConnectionIsOpen();

            Logger.LogSay("Beginning Transaction");

            Transaction = Connection?.BeginTransaction();
        }

        /// <inheritdoc />
        public override void RollbackTransaction()
        {
            if (Transaction == null) return;

            Logger.LogSay("Rolling back transaction");
            Transaction.Rollback();
            Transaction.Dispose();
            WasCommitted = true;
            Transaction = null;
        }

        /// <inheritdoc />
        public override void CommitTransaction()
        {
            if (Transaction == null) return;

            Logger.LogSay("Committing Transaction");
            Transaction.Commit();
            Transaction.Dispose();
            WasCommitted = true;
            Transaction = null;
        }

        /// <inheritdoc />
        protected override void Dispose(bool isDisposing)
        {
            if (!isDisposing || _disposed)
                return;

            _disposed = true;

            RollbackTransaction();
            EnsureConnectionIsClosed();
            if ((_connection != null || (_lazyConnection.IsValueCreated && Connection != null)))
            {
                Connection.Dispose();
            }
        }

        /// <summary>
        /// Creates a database command for the specified command text.
        /// </summary>
        /// <param name="commandText">The command text.</param>
        /// <returns>The database command.</returns>
        protected virtual IDbCommand CreateCommand(string commandText)
        {
            return CreateCommand(commandText, Connection, Transaction);
        }

        /// <summary>
        /// Creates a database command for the specified command text, connection, and transaction.
        /// </summary>
        /// <param name="commandText">The command text.</param>
        /// <param name="connection">The database connection.</param>
        /// <param name="transaction">The database transaction.</param>
        /// <returns>The database command.</returns>
        protected virtual IDbCommand CreateCommand(string commandText, IDbConnection connection, IDbTransaction transaction)
        {
            IDbCommand result;
            if (DbProviderFactory != null)
            {
                result = DbProviderFactory.CreateCommand();
                Debug.Assert(result != null, nameof(result) + " != null");
                result!.Connection = connection;
                if (transaction != null)
                    result.Transaction = transaction;
                result.CommandText = commandText;
            }
            else
            {
                throw new InvalidOperationException("DbProviderFactory not initialized.");
            }

            if (Options.Timeout != null)
            {
                result.CommandTimeout = (int) Options.Timeout.Value.TotalSeconds;
            }

            return result;
        }

        /// <inheritdoc />
        public override void Execute(string template, params object[] args)
        {
            Process(string.Format(template, args));
        }

        /// <summary>
        /// Formats the schema name for use in SQL queries.
        /// </summary>
        protected virtual string FormatSchemaName(string schemaName)
        {
            return FormatHelper.FormatSqlEscape(schemaName);
        }

        /// <summary>
        /// Formats a database object name for use in SQL queries.
        /// </summary>
        protected virtual string FormatName(string name)
        {
            return FormatHelper.FormatSqlEscape(name);
        }

        /// <inheritdoc />
        public override bool SchemaExists(string schemaName)
        {
            return Exists(SchemaExistsQuery, FormatSchemaName(schemaName));
        }

        /// <inheritdoc />
        public override bool TableExists(string schemaName, string tableName)
        {
            return Exists(TableExistsQuery, FormatSchemaName(schemaName), FormatName(tableName));
        }

        /// <inheritdoc />
        public override bool ColumnExists(string schemaName, string tableName, string columnName)
        {
            return Exists(ColumnExistsQuery, FormatSchemaName(schemaName), FormatName(tableName), FormatName(columnName));
        }

        /// <inheritdoc />
        public override bool ConstraintExists(string schemaName, string tableName, string constraintName)
        {
            return Exists(ConstraintExistsQuery, FormatSchemaName(schemaName), FormatName(tableName), FormatName(constraintName));
        }

        /// <inheritdoc />
        public override bool IndexExists(string schemaName, string tableName, string indexName)
        {
            return Exists(IndexExistsQuery, FormatSchemaName(schemaName), FormatName(tableName), FormatName(indexName));
        }

        /// <inheritdoc />
        public override bool SequenceExists(string schemaName, string sequenceName)
        {
            return Exists(SequenceExistsQuery, FormatSchemaName(schemaName), FormatName(sequenceName));
        }

        /// <inheritdoc />
        public override bool DefaultValueExists(string schemaName, string tableName, string columnName, object defaultValue)
        {
            var defaultValueAsString = $"%{FormatHelper.FormatSqlEscape(defaultValue.ToString())}%";
            return Exists(DefaultValueExistsQuery, FormatSchemaName(schemaName), FormatName(tableName), FormatName(columnName), defaultValueAsString);
        }

        /// <inheritdoc />
        public override DataSet Read(string template, params object[] args)
        {
            EnsureConnectionIsOpen();

            using (var command = CreateCommand(string.Format(template, args)))
            using (var reader = command.ExecuteReader())
            {
                return reader.ReadDataSet();
            }
        }

        /// <inheritdoc />
        [StringFormatMethod("template")]
        public override bool Exists(string template, params object[] args)
        {
            EnsureConnectionIsOpen();

            using (var command = CreateCommand(string.Format(template, args)))
            using (var reader = command.ExecuteReader())
            {
                try
                {
                    return reader.Read();
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <inheritdoc />
        public override DataSet ReadTableData(string schemaName, string tableName)
        {
            return Read(ReadTableDataQuery, Quoter.QuoteTableName(tableName, schemaName));
        }

        /// <inheritdoc />
        protected override void Process(string sql)
        {
            Logger.LogSql(sql);

            if (Options.PreviewOnly || string.IsNullOrEmpty(sql))
            {
                return;
            }

            EnsureConnectionIsOpen();

            using (var command = CreateCommand(sql))
            {
                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    ReThrowWithSql(ex, sql);
                }
            }
        }

        /// <inheritdoc />
        public override void Process(PerformDBOperationExpression expression)
        {
            var message = string.IsNullOrEmpty(expression.Description)
                ? "Performing DB Operation"
                : $"Performing DB Operation: {expression.Description}";
            Logger.LogSay(message);

            if (Options.PreviewOnly)
            {
                return;
            }

            EnsureConnectionIsOpen();

            expression.Operation?.Invoke(Connection, Transaction);
        }
    }
}
