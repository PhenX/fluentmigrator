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

using System.Threading.Tasks;

using DatabaseSchemaReader.DataSchema;

using NUnit.Framework;

using VerifyNUnit;

namespace FluentMigrator.MigrationGenerator.Tests
{
    [TestFixture]
    public class MigrationCodeGeneratorTests
    {
        [Test]
        public Task GenerateSingleMigration_SimpleTable()
        {
            var schema = CreateSimpleTableSchema();
            var options = new MigrationGeneratorOptions
            {
                Namespace = "TestMigrations",
                Mode = GenerationMode.SingleMigration
            };

            var generator = new MigrationCodeGenerator(options, schema);
            var files = generator.GenerateFromSchema(schema);

            return Verifier.Verify(files[0].Content);
        }

        [Test]
        public Task GenerateSingleMigration_TableWithAllColumnTypes()
        {
            var schema = CreateTableWithAllColumnTypes();
            var options = new MigrationGeneratorOptions
            {
                Namespace = "TestMigrations",
                Mode = GenerationMode.SingleMigration
            };

            var generator = new MigrationCodeGenerator(options, schema);
            var files = generator.GenerateFromSchema(schema);

            return Verifier.Verify(files[0].Content);
        }

        [Test]
        public Task GenerateSingleMigration_TableWithIndexes()
        {
            var schema = CreateTableWithIndexes();
            var options = new MigrationGeneratorOptions
            {
                Namespace = "TestMigrations",
                Mode = GenerationMode.SingleMigration
            };

            var generator = new MigrationCodeGenerator(options, schema);
            var files = generator.GenerateFromSchema(schema);

            return Verifier.Verify(files[0].Content);
        }

        [Test]
        public Task GenerateSingleMigration_TableWithForeignKeys()
        {
            var schema = CreateSchemaWithForeignKeys();
            var options = new MigrationGeneratorOptions
            {
                Namespace = "TestMigrations",
                Mode = GenerationMode.SingleMigration
            };

            var generator = new MigrationCodeGenerator(options, schema);
            var files = generator.GenerateFromSchema(schema);

            return Verifier.Verify(files[0].Content);
        }

        [Test]
        public Task GenerateSingleMigration_TableWithUniqueConstraints()
        {
            var schema = CreateTableWithUniqueConstraints();
            var options = new MigrationGeneratorOptions
            {
                Namespace = "TestMigrations",
                Mode = GenerationMode.SingleMigration
            };

            var generator = new MigrationCodeGenerator(options, schema);
            var files = generator.GenerateFromSchema(schema);

            return Verifier.Verify(files[0].Content);
        }

        [Test]
        public Task GenerateOnePerTable_MultipleTables()
        {
            var schema = CreateSchemaWithForeignKeys();
            var options = new MigrationGeneratorOptions
            {
                Namespace = "TestMigrations",
                Mode = GenerationMode.OnePerTable
            };

            var generator = new MigrationCodeGenerator(options, schema);
            var files = generator.GenerateFromSchema(schema);

            return Verifier.Verify(files);
        }

        [Test]
        public Task GenerateSingleMigration_WithTableFilters()
        {
            var schema = CreateSchemaWithMultipleTables();
            var options = new MigrationGeneratorOptions
            {
                Namespace = "TestMigrations",
                Mode = GenerationMode.SingleMigration,
                IncludeTables = { "Users", "Posts" },
                ExcludeTables = { "Posts" }
            };

            var generator = new MigrationCodeGenerator(options, schema);
            var files = generator.GenerateFromSchema(schema);

            return Verifier.Verify(files[0].Content);
        }

        private static DatabaseSchema CreateSimpleTableSchema()
        {
            var schema = new DatabaseSchema(null, null);
            var table = schema.AddTable("Users");

            var idColumn = table.AddColumn("Id", "INTEGER");
            idColumn.IsPrimaryKey = true;
            idColumn.IsAutoNumber = true;

            var nameColumn = table.AddColumn("Name", "NVARCHAR");
            nameColumn.Length = 100;
            nameColumn.Nullable = false;

            var emailColumn = table.AddColumn("Email", "NVARCHAR");
            emailColumn.Length = 255;
            emailColumn.Nullable = true;

            // Set primary key via constraint
            var pkConstraint = new DatabaseConstraint
            {
                Name = "PK_Users",
                TableName = "Users",
                ConstraintType = ConstraintType.PrimaryKey
            };
            pkConstraint.Columns.Add("Id");
            table.AddConstraint(pkConstraint);

            return schema;
        }

        private static DatabaseSchema CreateTableWithAllColumnTypes()
        {
            var schema = new DatabaseSchema(null, null);
            var table = schema.AddTable("AllTypes");

            var idCol = table.AddColumn("Id", "INTEGER");
            idCol.IsPrimaryKey = true;
            idCol.IsAutoNumber = true;

            table.AddColumn("IntCol", "INT").Nullable = true;
            table.AddColumn("BigIntCol", "BIGINT").Nullable = true;
            table.AddColumn("SmallIntCol", "SMALLINT").Nullable = true;
            table.AddColumn("TinyIntCol", "TINYINT").Nullable = true;
            table.AddColumn("BoolCol", "BIT").Nullable = true;

            var decimalCol = table.AddColumn("DecimalCol", "DECIMAL");
            decimalCol.Precision = 18;
            decimalCol.Scale = 2;
            decimalCol.Nullable = true;

            table.AddColumn("FloatCol", "FLOAT").Nullable = true;
            table.AddColumn("DateCol", "DATE").Nullable = true;
            table.AddColumn("TimeCol", "TIME").Nullable = true;
            table.AddColumn("DateTimeCol", "DATETIME").Nullable = true;
            table.AddColumn("GuidCol", "UNIQUEIDENTIFIER").Nullable = true;

            var binaryCol = table.AddColumn("BinaryCol", "VARBINARY");
            binaryCol.Length = 100;
            binaryCol.Nullable = true;

            table.AddColumn("TextCol", "TEXT").Nullable = true;
            table.AddColumn("XmlCol", "XML").Nullable = true;

            var charCol = table.AddColumn("CharCol", "CHAR");
            charCol.Length = 10;
            charCol.Nullable = true;

            var varcharCol = table.AddColumn("VarcharCol", "VARCHAR");
            varcharCol.Length = 50;
            varcharCol.Nullable = true;

            var pkConstraint = new DatabaseConstraint
            {
                Name = "PK_AllTypes",
                TableName = "AllTypes",
                ConstraintType = ConstraintType.PrimaryKey
            };
            pkConstraint.Columns.Add("Id");
            table.AddConstraint(pkConstraint);

            return schema;
        }

        private static DatabaseSchema CreateTableWithIndexes()
        {
            var schema = new DatabaseSchema(null, null);
            var table = schema.AddTable("Products");

            var idColumn = table.AddColumn("Id", "INTEGER");
            idColumn.IsPrimaryKey = true;
            idColumn.IsAutoNumber = true;

            var nameColumn = table.AddColumn("Name", "NVARCHAR");
            nameColumn.Length = 100;
            nameColumn.Nullable = false;

            var skuColumn = table.AddColumn("Sku", "NVARCHAR");
            skuColumn.Length = 50;
            skuColumn.Nullable = false;

            var priceColumn = table.AddColumn("Price", "DECIMAL");
            priceColumn.Precision = 18;
            priceColumn.Scale = 2;
            priceColumn.Nullable = false;

            var categoryIdColumn = table.AddColumn("CategoryId", "INTEGER");
            categoryIdColumn.Nullable = true;

            var pkConstraint = new DatabaseConstraint
            {
                Name = "PK_Products",
                TableName = "Products",
                ConstraintType = ConstraintType.PrimaryKey
            };
            pkConstraint.Columns.Add("Id");
            table.AddConstraint(pkConstraint);

            // Add indexes
            var nameIndex = new DatabaseIndex { Name = "IX_Products_Name", TableName = "Products" };
            nameIndex.Columns.Add(nameColumn);
            table.AddIndex(nameIndex);

            var skuIndex = new DatabaseIndex { Name = "IX_Products_Sku", TableName = "Products", IsUnique = true };
            skuIndex.Columns.Add(skuColumn);
            table.AddIndex(skuIndex);

            return schema;
        }

        private static DatabaseSchema CreateTableWithUniqueConstraints()
        {
            var schema = new DatabaseSchema(null, null);
            var table = schema.AddTable("Users");

            var idColumn = table.AddColumn("Id", "INTEGER");
            idColumn.IsPrimaryKey = true;
            idColumn.IsAutoNumber = true;

            var usernameColumn = table.AddColumn("Username", "NVARCHAR");
            usernameColumn.Length = 50;
            usernameColumn.Nullable = false;

            var emailColumn = table.AddColumn("Email", "NVARCHAR");
            emailColumn.Length = 255;
            emailColumn.Nullable = false;

            var pkConstraint = new DatabaseConstraint
            {
                Name = "PK_Users",
                TableName = "Users",
                ConstraintType = ConstraintType.PrimaryKey
            };
            pkConstraint.Columns.Add("Id");
            table.AddConstraint(pkConstraint);

            // Add unique constraints
            var usernameUniqueConstraint = new DatabaseConstraint
            {
                Name = "UK_Users_Username",
                TableName = "Users",
                ConstraintType = ConstraintType.UniqueKey
            };
            usernameUniqueConstraint.Columns.Add("Username");
            table.AddConstraint(usernameUniqueConstraint);

            var emailUniqueConstraint = new DatabaseConstraint
            {
                Name = "UK_Users_Email",
                TableName = "Users",
                ConstraintType = ConstraintType.UniqueKey
            };
            emailUniqueConstraint.Columns.Add("Email");
            table.AddConstraint(emailUniqueConstraint);

            return schema;
        }

        private static DatabaseSchema CreateSchemaWithForeignKeys()
        {
            var schema = new DatabaseSchema(null, null);

            // Create Categories table first (no dependencies)
            var categoriesTable = schema.AddTable("Categories");
            var categoryIdColumn = categoriesTable.AddColumn("Id", "INTEGER");
            categoryIdColumn.IsPrimaryKey = true;
            categoryIdColumn.IsAutoNumber = true;
            var categoryNameColumn = categoriesTable.AddColumn("Name", "NVARCHAR");
            categoryNameColumn.Length = 100;
            categoryNameColumn.Nullable = false;

            var pkCategories = new DatabaseConstraint
            {
                Name = "PK_Categories",
                TableName = "Categories",
                ConstraintType = ConstraintType.PrimaryKey
            };
            pkCategories.Columns.Add("Id");
            categoriesTable.AddConstraint(pkCategories);

            // Create Users table (no dependencies)
            var usersTable = schema.AddTable("Users");
            var userIdColumn = usersTable.AddColumn("Id", "INTEGER");
            userIdColumn.IsPrimaryKey = true;
            userIdColumn.IsAutoNumber = true;
            var userNameColumn = usersTable.AddColumn("Name", "NVARCHAR");
            userNameColumn.Length = 100;
            userNameColumn.Nullable = false;

            var pkUsers = new DatabaseConstraint
            {
                Name = "PK_Users",
                TableName = "Users",
                ConstraintType = ConstraintType.PrimaryKey
            };
            pkUsers.Columns.Add("Id");
            usersTable.AddConstraint(pkUsers);

            // Create Posts table (depends on Users and Categories)
            var postsTable = schema.AddTable("Posts");
            var postIdColumn = postsTable.AddColumn("Id", "INTEGER");
            postIdColumn.IsPrimaryKey = true;
            postIdColumn.IsAutoNumber = true;
            var postTitleColumn = postsTable.AddColumn("Title", "NVARCHAR");
            postTitleColumn.Length = 200;
            postTitleColumn.Nullable = false;
            var postUserIdColumn = postsTable.AddColumn("UserId", "INTEGER");
            postUserIdColumn.Nullable = false;
            var postCategoryIdColumn = postsTable.AddColumn("CategoryId", "INTEGER");
            postCategoryIdColumn.Nullable = true;

            var pkPosts = new DatabaseConstraint
            {
                Name = "PK_Posts",
                TableName = "Posts",
                ConstraintType = ConstraintType.PrimaryKey
            };
            pkPosts.Columns.Add("Id");
            postsTable.AddConstraint(pkPosts);

            // Add foreign keys
            var userFk = new DatabaseConstraint
            {
                Name = "FK_Posts_Users",
                TableName = "Posts",
                RefersToTable = "Users",
                ConstraintType = ConstraintType.ForeignKey
            };
            userFk.Columns.Add("UserId");
            postsTable.AddConstraint(userFk);

            var categoryFk = new DatabaseConstraint
            {
                Name = "FK_Posts_Categories",
                TableName = "Posts",
                RefersToTable = "Categories",
                ConstraintType = ConstraintType.ForeignKey,
                DeleteRule = "SET NULL"
            };
            categoryFk.Columns.Add("CategoryId");
            postsTable.AddConstraint(categoryFk);

            return schema;
        }

        private static DatabaseSchema CreateSchemaWithMultipleTables()
        {
            var schema = new DatabaseSchema(null, null);

            var usersTable = schema.AddTable("Users");
            var userIdColumn = usersTable.AddColumn("Id", "INTEGER");
            userIdColumn.IsPrimaryKey = true;
            userIdColumn.IsAutoNumber = true;

            var pkUsers = new DatabaseConstraint
            {
                Name = "PK_Users",
                TableName = "Users",
                ConstraintType = ConstraintType.PrimaryKey
            };
            pkUsers.Columns.Add("Id");
            usersTable.AddConstraint(pkUsers);

            var postsTable = schema.AddTable("Posts");
            var postIdColumn = postsTable.AddColumn("Id", "INTEGER");
            postIdColumn.IsPrimaryKey = true;
            postIdColumn.IsAutoNumber = true;

            var pkPosts = new DatabaseConstraint
            {
                Name = "PK_Posts",
                TableName = "Posts",
                ConstraintType = ConstraintType.PrimaryKey
            };
            pkPosts.Columns.Add("Id");
            postsTable.AddConstraint(pkPosts);

            var categoriesTable = schema.AddTable("Categories");
            var categoryIdColumn = categoriesTable.AddColumn("Id", "INTEGER");
            categoryIdColumn.IsPrimaryKey = true;
            categoryIdColumn.IsAutoNumber = true;

            var pkCategories = new DatabaseConstraint
            {
                Name = "PK_Categories",
                TableName = "Categories",
                ConstraintType = ConstraintType.PrimaryKey
            };
            pkCategories.Columns.Add("Id");
            categoriesTable.AddConstraint(pkCategories);

            return schema;
        }
    }
}
