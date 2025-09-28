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
using System.Linq;
using FluentMigrator.Builders.Upsert;
using FluentMigrator.Expressions;
using FluentMigrator.Model;

namespace FluentMigrator.Builders.Upsert
{
    /// <summary>
    /// An expression builder for a <see cref="UpsertDataExpression"/>
    /// </summary>
    public class UpsertDataExpressionBuilder : ExpressionBuilderBase<UpsertDataExpression>, 
        IUpsertDataOrInSchemaSyntax, IUpsertDataSyntax, IUpsertRowSyntax, IUpsertUpdateColumnsSyntax
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpsertDataExpressionBuilder"/> class.
        /// </summary>
        /// <param name="expression">The underlying expression</param>
        public UpsertDataExpressionBuilder(UpsertDataExpression expression) : base(expression)
        {
        }

        /// <inheritdoc />
        public IUpsertDataSyntax InSchema(string schemaName)
        {
            Expression.SchemaName = schemaName;
            return this;
        }

        /// <inheritdoc />
        public IUpsertRowSyntax MatchOn(params string[] columnNames)
        {
            Expression.MatchColumns.Clear();
            Expression.MatchColumns.AddRange(columnNames);
            return this;
        }

        /// <inheritdoc />
        public IUpsertUpdateColumnsSyntax Row(object dataAsAnonymousType)
        {
            var data = GetData<InsertionDataDefinition>(dataAsAnonymousType);
            Expression.Rows.Add(data);
            return this;
        }

        /// <inheritdoc />
        public IUpsertUpdateColumnsSyntax Rows(params object[] dataAsAnonymousTypes)
        {
            foreach (var dataAsAnonymousType in dataAsAnonymousTypes)
            {
                var data = GetData<InsertionDataDefinition>(dataAsAnonymousType);
                Expression.Rows.Add(data);
            }
            return this;
        }

        /// <inheritdoc />
        public void UpdateColumns(params string[] columnNames)
        {
            Expression.UpdateColumns = columnNames?.ToList();
        }

        /// <inheritdoc />
        public void UpdateColumns(object updateValues)
        {
            Expression.UpdateValues = GetData<List<KeyValuePair<string, object>>>(updateValues);
        }

        /// <inheritdoc />
        public void IgnoreInsertIfExists()
        {
            Expression.IgnoreInsertIfExists = true;
        }
    }
}