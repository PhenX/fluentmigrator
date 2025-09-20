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
using FluentMigrator.Builders.Schema;
using FluentMigrator.Infrastructure;

namespace FluentMigrator.Builders.If
{
    /// <summary>
    /// The starting point for conditional schema-based expressions
    /// </summary>
    public interface IIfExpressionRoot : IFluentSyntax
    {
        /// <summary>
        /// Specify actions to execute if the condition is true
        /// </summary>
        /// <param name="action">The action to execute if condition evaluates to true</param>
        /// <returns>The fluent interface</returns>
        IFluentSyntax Then(Action<IIfThenMigrationExpressionRoot> action);
    }
}