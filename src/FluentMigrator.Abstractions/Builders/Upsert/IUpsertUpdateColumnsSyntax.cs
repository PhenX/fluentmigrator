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

using FluentMigrator.Infrastructure;

namespace FluentMigrator.Builders.Upsert
{
    /// <summary>
    /// Optionally specify which columns to update on match (terminal syntax)
    /// </summary>
    public interface IUpsertUpdateColumnsSyntax : IFluentSyntax
    {
        /// <summary>
        /// Specify which columns to update when a match is found. 
        /// If not called, all columns except match columns will be updated.
        /// </summary>
        /// <param name="columnNames">The column names to update on match</param>
        void UpdateColumns(params string[] columnNames);

        /// <summary>
        /// Specify the exact values to use when updating matched rows.
        /// This allows using RawSql expressions and overriding row data values for updates.
        /// Cannot be used together with UpdateColumns (string array version) or IgnoreInsertIfExists.
        /// </summary>
        /// <param name="updateValues">Anonymous object containing column names and their update values, supports RawSql expressions</param>
        void UpdateColumns(object updateValues);

        /// <summary>
        /// Configure the upsert to ignore the insert if the row already exists (INSERT IGNORE mode).
        /// When enabled, existing rows are not updated, only new rows are inserted.
        /// Cannot be used together with UpdateColumns.
        /// </summary>
        void IgnoreInsertIfExists();
    }
}