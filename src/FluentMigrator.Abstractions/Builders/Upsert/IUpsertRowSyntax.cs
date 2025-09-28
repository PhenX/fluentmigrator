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
    /// Specify the rows to upsert and optionally the columns to update
    /// </summary>
    public interface IUpsertRowSyntax : IFluentSyntax
    {
        /// <summary>
        /// Specify a single row to upsert
        /// </summary>
        /// <param name="dataAsAnonymousType">The row data as an anonymous object</param>
        /// <returns>The next step</returns>
        IUpsertUpdateColumnsSyntax Row(object dataAsAnonymousType);

        /// <summary>
        /// Specify multiple rows to upsert with the same structure
        /// </summary>
        /// <param name="dataAsAnonymousTypes">The rows data as anonymous objects</param>
        /// <returns>The next step</returns>
        IUpsertUpdateColumnsSyntax Rows(params object[] dataAsAnonymousTypes);
    }
}