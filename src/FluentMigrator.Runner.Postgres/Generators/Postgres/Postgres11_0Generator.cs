#region License
// Copyright (c) 2007-2018, FluentMigrator Project
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

using System.Collections.Generic;
using System.Text;

using FluentMigrator.Expressions;
using FluentMigrator.Infrastructure.Extensions;
using FluentMigrator.Model;
using FluentMigrator.Postgres;

using JetBrains.Annotations;

using Microsoft.Extensions.Options;

namespace FluentMigrator.Runner.Generators.Postgres
{
    public class Postgres11_0Generator : PostgresGenerator
    {
        public Postgres11_0Generator([NotNull] PostgresQuoter quoter)
            : this(quoter, new OptionsWrapper<GeneratorOptions>(new GeneratorOptions()))
        {
        }

        public Postgres11_0Generator([NotNull] PostgresQuoter quoter, [NotNull] IOptions<GeneratorOptions> generatorOptions)
            : base(new Postgres11_0Column(quoter, new Postgres92.Postgres92TypeMap()), quoter, generatorOptions)
        {
        }

        protected Postgres11_0Generator([NotNull] PostgresQuoter quoter, [NotNull] IOptions<GeneratorOptions> generatorOptions, [NotNull] ITypeMap typeMap)
            : base(new Postgres10_0Column(quoter, typeMap), quoter, generatorOptions)
        {
        }

        public virtual string GetIncludeString(CreateIndexExpression column)
        {
            var includes = column.GetAdditionalFeature<IList<PostgresIndexIncludeDefinition>>(PostgresExtensions.IncludesList);

            if (includes == null || includes.Count == 0)
            {
                return string.Empty;
            }

            var result = new StringBuilder(" INCLUDE (");
            result.Append(Quoter.QuoteColumnName(includes[0].Name));

            for (var i = 1; i < includes.Count; i++)
            {
                result
                    .Append(", ")
                    .Append(Quoter.QuoteColumnName(includes[i].Name));
            }

            return result
                .Append(")")
                .ToString();
        }

        /// <inheritdoc />
        public override string Generate(CreateIndexExpression expression)
        {
            var result = new StringBuilder("CREATE");
            if (expression.Index.IsUnique)
                result.Append(" UNIQUE");

            result.Append(" INDEX {0} ON {1} (");

            var first = true;
            foreach (var column in expression.Index.Columns)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    result.Append(",");
                }

                result.Append(Quoter.QuoteColumnName(column.Name));
                result.Append(column.Direction == Direction.Ascending ? " ASC" : " DESC");
            }

            result.Append(");")
                .Append(GetIncludeString(expression));

            return string.Format(result.ToString(), Quoter.QuoteIndexName(expression.Index.Name), Quoter.QuoteTableName(expression.Index.TableName, expression.Index.SchemaName));
        }
    }
}
