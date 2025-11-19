#region License
// Copyright (c) 2018, Fluent Migrator Project
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

using FluentMigrator.Runner.Processors.Oracle;
using NUnit.Framework;

namespace FluentMigrator.Tests.Unit.BatchParser;

/// <summary>
/// Tests for OracleSqlStatementSplitter to ensure proper statement separation
/// for both simple SQL and complex PL/SQL blocks
/// </summary>
[TestFixture]
public class OracleSqlStatementSplitterTests
{
    [Test]
    public void SplitStatements_WithNullInput_ReturnsEmptyList()
    {
        var result = OracleSqlStatementSplitter.SplitStatements(null);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void SplitStatements_WithEmptyInput_ReturnsEmptyList()
    {
        var result = OracleSqlStatementSplitter.SplitStatements(string.Empty);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void SplitStatements_WithWhitespaceOnly_ReturnsEmptyList()
    {
        var result = OracleSqlStatementSplitter.SplitStatements("   \n\r\t  ");

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void SplitStatements_WithSingleSimpleStatement_ReturnsSingleStatement()
    {
        // language=sql
        const string sql = "SELECT * FROM users;";

        var result = OracleSqlStatementSplitter.SplitStatements(sql);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo("SELECT * FROM users"));
    }

    [Test]
    public void SplitStatements_WithMultipleSimpleStatements_ReturnsCorrectStatements()
    {
        // language=sql
        const string sql = """
            DELETE FROM temp_table;
            INSERT INTO users (name, email) VALUES ('John', 'john@example.com');
            UPDATE users SET active = 1 WHERE id = 1;
            """;

        var result = OracleSqlStatementSplitter.SplitStatements(sql);

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result[0], Is.EqualTo("DELETE FROM temp_table"));
        Assert.That(result[1], Is.EqualTo("INSERT INTO users (name, email) VALUES ('John', 'john@example.com')"));
        Assert.That(result[2], Is.EqualTo("UPDATE users SET active = 1 WHERE id = 1"));
    }

    [Test]
    public void SplitStatements_WithSemicolonsOnSingleLine_SplitCorrectly()
    {
        // language=sql
        const string sql = "DELETE FROM SameTableFK WHERE Id = 20 AND ParentId = 2;DELETE FROM SameTableFK WHERE Id = 10 AND ParentId = 1;DELETE FROM SameTableFK WHERE Id = 2;";

        var result = OracleSqlStatementSplitter.SplitStatements(sql);

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result[0], Is.EqualTo("DELETE FROM SameTableFK WHERE Id = 20 AND ParentId = 2"));
        Assert.That(result[1], Is.EqualTo("DELETE FROM SameTableFK WHERE Id = 10 AND ParentId = 1"));
        Assert.That(result[2], Is.EqualTo("DELETE FROM SameTableFK WHERE Id = 2"));
    }

    [Test]
    public void SplitStatements_WithSlashTerminator_SplitsCorrectly()
    {
        // language=sql
        const string sql = """
            CREATE TABLE test (id NUMBER)
            /
            INSERT INTO test VALUES (1)
            /
            """;

        var result = OracleSqlStatementSplitter.SplitStatements(sql);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0], Is.EqualTo("CREATE TABLE test (id NUMBER)"));
        Assert.That(result[1], Is.EqualTo("INSERT INTO test VALUES (1)"));
    }

    [Test]
    public void SplitStatements_WithSimpleBeginEndBlock_KeepsAsOneStatement()
    {
        // language=sql
        const string sql = """
            BEGIN
                INSERT INTO users VALUES (1, 'Test');
                UPDATE users SET active = 1;
            END;
            """;

        var result = OracleSqlStatementSplitter.SplitStatements(sql);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Does.Contain("BEGIN"));
        Assert.That(result[0], Does.Contain("INSERT INTO users VALUES (1, 'Test')"));
        Assert.That(result[0], Does.Contain("UPDATE users SET active = 1"));
        Assert.That(result[0], Does.Contain("END"));
    }

    [Test]
    public void SplitStatements_WithComplexPlSqlBlockFromExample_KeepsAsOneStatement()
    {
        // language=sql
        const string sql = "BEGIN EXECUTE IMMEDIATE 'CREATE TABLE {0} ({1})'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;";

        var result = OracleSqlStatementSplitter.SplitStatements(sql);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Does.Contain("BEGIN"));
        Assert.That(result[0], Does.Contain("EXCEPTION"));
        Assert.That(result[0], Does.Contain("END IF"));
        Assert.That(result[0], Does.Contain("END"));
    }

    [Test]
    public void SplitStatements_WithNestedBeginEndBlocks_KeepsAsOneStatement()
    {
        // language=sql
        const string sql = """
            BEGIN
                INSERT INTO log VALUES ('Start');
                BEGIN
                    UPDATE users SET active = 1;
                    DELETE FROM temp WHERE id > 100;
                END;
                INSERT INTO log VALUES ('Complete');
            END;
            """;

        var result = OracleSqlStatementSplitter.SplitStatements(sql);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Does.Contain("INSERT INTO log VALUES ('Start')"));
        Assert.That(result[0], Does.Contain("UPDATE users SET active = 1"));
        Assert.That(result[0], Does.Contain("DELETE FROM temp WHERE id > 100"));
    }

    [Test]
    public void SplitStatements_WithIfElseEndIfStructure_DoesNotSplitOnEndIf()
    {
        // language=sql
        const string sql = """
            BEGIN
                IF user_count > 0 THEN
                    UPDATE users SET status = 'active';
                ELSE
                    INSERT INTO users VALUES (1, 'default');
                END IF;
                COMMIT;
            END;
            """;

        var result = OracleSqlStatementSplitter.SplitStatements(sql);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Does.Contain("IF user_count > 0 THEN"));
        Assert.That(result[0], Does.Contain("END IF"));
        Assert.That(result[0], Does.Contain("COMMIT"));
    }

    [Test]
    public void SplitStatements_WithProcedureCreation_KeepsAsOneStatement()
    {
        // language=sql
        const string sql = """
            CREATE OR REPLACE PROCEDURE test_proc(p_id IN NUMBER) AS
            BEGIN
                UPDATE users SET last_access = SYSDATE WHERE id = p_id;
                INSERT INTO audit_log VALUES (p_id, SYSDATE);
            END test_proc;
            """;

        var result = OracleSqlStatementSplitter.SplitStatements(sql);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Does.Contain("CREATE OR REPLACE PROCEDURE"));
        Assert.That(result[0], Does.Contain("UPDATE users"));
        Assert.That(result[0], Does.Contain("INSERT INTO audit_log"));
    }

    [Test]
    public void SplitStatements_WithMixedStatementsAndPlSqlBlocks_SplitsCorrectly()
    {
        // language=sql
        const string sql = """
            CREATE TABLE test_table (id NUMBER, name VARCHAR2(50));

            INSERT INTO test_table VALUES (1, 'Initial');

            BEGIN
                FOR i IN 2..5 LOOP
                    INSERT INTO test_table VALUES (i, 'Loop ' || i);
                END LOOP;
                COMMIT;
            END;

            SELECT COUNT(*) FROM test_table;
            """;

        var result = OracleSqlStatementSplitter.SplitStatements(sql);

        Assert.That(result, Has.Count.EqualTo(4));
        Assert.That(result[0], Does.Contain("CREATE TABLE test_table"));
        Assert.That(result[1], Does.Contain("INSERT INTO test_table VALUES (1, 'Initial')"));
        Assert.That(result[2], Does.Contain("BEGIN"));
        Assert.That(result[2], Does.Contain("FOR i IN 2..5 LOOP"));
        Assert.That(result[2], Does.Contain("END LOOP"));
        Assert.That(result[2], Does.Contain("END"));
        Assert.That(result[3], Does.Contain("SELECT COUNT(*) FROM test_table"));
    }

    [Test]
    public void SplitStatements_WithStringLiteralsContainingSemicolons_DoesNotSplitInsideStrings()
    {
        // language=sql
        const string sql = """
            INSERT INTO messages (content) VALUES ('Hello; World; Test');
            UPDATE users SET bio = 'Software Engineer; Team Lead; Manager';
            """;

        var result = OracleSqlStatementSplitter.SplitStatements(sql);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0], Is.EqualTo("INSERT INTO messages (content) VALUES ('Hello; World; Test')"));
        Assert.That(result[1], Is.EqualTo("UPDATE users SET bio = 'Software Engineer; Team Lead; Manager'"));
    }

    [Test]
    public void SplitStatements_WithCommentsContainingSemicolons_IgnoresComments()
    {
        // language=sql
        const string sql = """
            -- This is a comment with; semicolons; inside
            CREATE TABLE users (id NUMBER);
            /* Multi-line comment
               with; semicolons; here */
            INSERT INTO users VALUES (1);
            """;

        var result = OracleSqlStatementSplitter.SplitStatements(sql);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0], Is.EqualTo("CREATE TABLE users (id NUMBER)"));
        Assert.That(result[1], Is.EqualTo("INSERT INTO users VALUES (1)"));
    }

    [Test]
    public void SplitStatements_WithDeclareBlock_KeepsAsOneStatement()
    {
        // language=sql
        const string sql = """
            DECLARE
                v_count NUMBER;
            BEGIN
                SELECT COUNT(*) INTO v_count FROM users;
                IF v_count > 0 THEN
                    DELETE FROM temp_users;
                END IF;
            END;
            """;

        var result = OracleSqlStatementSplitter.SplitStatements(sql);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Does.Contain("DECLARE"));
        Assert.That(result[0], Does.Contain("v_count NUMBER"));
        Assert.That(result[0], Does.Contain("SELECT COUNT(*)"));
    }

    [Test]
    public void SplitStatements_WithTriggerCreation_KeepsAsOneStatement()
    {
        // language=sql
        const string sql = """
            CREATE OR REPLACE TRIGGER user_audit_trigger
            AFTER INSERT OR UPDATE ON users
            FOR EACH ROW
            BEGIN
                INSERT INTO audit_log VALUES (:NEW.id, SYSDATE, USER);
            END;
            """;

        var result = OracleSqlStatementSplitter.SplitStatements(sql);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Does.Contain("CREATE OR REPLACE TRIGGER"));
        Assert.That(result[0], Does.Contain("FOR EACH ROW"));
        Assert.That(result[0], Does.Contain("INSERT INTO audit_log"));
    }
}

