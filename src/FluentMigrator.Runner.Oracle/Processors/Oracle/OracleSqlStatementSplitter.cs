using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FluentMigrator.Runner.Processors.Oracle;

/// <summary>
/// Splits complex Oracle SQL statements into individual executable statements for ADO.NET
/// </summary>
public static class OracleSqlStatementSplitter
{
    private const char Slash = '/';
    private const char Semicolon = ';';
    private const char SimpleQuote = '\'';
    private const char BackSlash = '\\';
    private const char Asterisk = '*';
    private const char Dash = '-';
    private const char CarriageReturn = '\r';
    private const char NewLine = '\n';
    private const char Zero = '\0';

    private static readonly string[] StatementTerminators = [
        ";",
        "/",
    ];

    private static readonly string[] PlsqlBlocks = [
        "BEGIN",
        "DECLARE",
        "CREATE OR REPLACE PROCEDURE",
        "CREATE OR REPLACE FUNCTION",
        "CREATE OR REPLACE PACKAGE",
        "CREATE OR REPLACE TRIGGER",
    ];

    /// <summary>
    /// Splits a complex SQL script into individual executable statements
    /// </summary>
    /// <param name="sqlScript">The complete SQL script to split</param>
    /// <returns>List of individual SQL statements ready for execution</returns>
    public static List<string> SplitStatements(string sqlScript)
    {
        if (string.IsNullOrWhiteSpace(sqlScript))
        {
            return [];
        }

        var statements = new List<string>();
        var currentStatement = new StringBuilder();
        var inPlsqlBlock = false;
        var inStringLiteral = false;
        var inComment = false;
        var inLineComment = false;
        var plsqlBlockDepth = 0;

        for (var i = 0; i < sqlScript.Length; i++)
        {
            var c = sqlScript[i];
            var nextChar = i < sqlScript.Length - 1 ? sqlScript[i + 1] : Zero;

            // Handle end of line for line comments
            if (inLineComment && c is CarriageReturn or NewLine)
            {
                inLineComment = false;
            }

            // Skip line comment content
            if (inLineComment)
            {
                continue;
            }

            // Handle line comment start
            if (!inStringLiteral && !inComment && c == Dash && nextChar == Dash)
            {
                inLineComment = true;
                i++; // Skip next dash
                continue;
            }

            // Handle multi-line comment start
            if (!inStringLiteral && c == Slash && nextChar == Asterisk)
            {
                inComment = true;
                i++; // Skip next character
                continue;
            }

            // Handle multi-line comment end
            if (inComment && c == Asterisk && nextChar == Slash)
            {
                inComment = false;
                i++; // Skip next character
                continue;
            }

            // Skip comment content
            if (inComment)
            {
                continue;
            }

            // Handle string literals
            if (c == SimpleQuote && (i == 0 || sqlScript[i - 1] != BackSlash))
            {
                inStringLiteral = !inStringLiteral;
                currentStatement.Append(c);
                continue;
            }

            // Process keywords when not in string literals
            if (!inStringLiteral && char.IsLetter(c))
            {
                var wordStart = i;
                while (i < sqlScript.Length && char.IsLetter(sqlScript[i]))
                {
                    i++;
                }
                i--; // Move back to the last letter of the word

                var word = sqlScript.Substring(wordStart, i - wordStart + 1);

                // Check for PL/SQL blocks
                if (IsWordAtPosition(sqlScript, wordStart, PlsqlBlocks))
                {
                    inPlsqlBlock = true;
                    plsqlBlockDepth++;
                }
                else if (word.Equals("BEGIN", StringComparison.OrdinalIgnoreCase) && (inPlsqlBlock || wordStart == 0 || IsWordStartOfLine(sqlScript, wordStart)))
                {
                    inPlsqlBlock = true;
                    plsqlBlockDepth++;
                }
                else if (word.Equals("END", StringComparison.OrdinalIgnoreCase) && inPlsqlBlock)
                {
                    // Check if we have END IF, END LOOP, etc.
                    var afterWordPos = i + 1;
                    while (afterWordPos < sqlScript.Length && char.IsWhiteSpace(sqlScript[afterWordPos]))
                    {
                        afterWordPos++;
                    }

                    var isControlEnd = false;
                    if (afterWordPos < sqlScript.Length - 1)
                    {
                        var nextWord = ExtractWord(sqlScript, afterWordPos);
                        isControlEnd = nextWord is "IF" or "LOOP" or "CASE";
                    }

                    if (!isControlEnd)
                    {
                        plsqlBlockDepth--;

                        // Reset block tracking when all blocks are closed
                        if (plsqlBlockDepth <= 0)
                        {
                            plsqlBlockDepth = 0;
                            inPlsqlBlock = false;
                        }

                        // Append semicolon to END
                        word += Semicolon;
                    }
                }

                currentStatement.Append(word);
                continue;
            }

            // Check for statement terminators
            if (!inStringLiteral && !inPlsqlBlock && c == Semicolon)
            {
                // We found a semicolon outside of a PL/SQL block or string literal
                // This is a statement terminator
                var statement = currentStatement.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(statement))
                {
                    statements.Add(statement);
                }
                currentStatement.Clear();
                continue;
            }

            // Check for / terminators on their own line
            if (!inStringLiteral && !inPlsqlBlock && c == Slash)
            {
                var isTerminator = false;

                if (c == Slash)
                {
                    isTerminator = IsSlashTerminator(sqlScript, i);
                    if (isTerminator)
                    {
                        i++; // Skip the slash
                    }
                }

                if (isTerminator)
                {
                    var statement = currentStatement.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(statement))
                    {
                        statements.Add(statement);
                    }
                    currentStatement.Clear();
                    continue;
                }
            }

            // Handle PL/SQL block terminators
            if (!inStringLiteral && inPlsqlBlock && plsqlBlockDepth == 0 && c == Semicolon)
            {
                // End of a PL/SQL block
                currentStatement.Append(c); // Include the semicolon in the statement
                var statement = currentStatement.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(statement))
                {
                    statements.Add(RemoveTerminator(statement));
                }
                currentStatement.Clear();
                inPlsqlBlock = false;
                continue;
            }

            // Append character to current statement
            currentStatement.Append(c);
        }

        // Add any remaining statement
        var finalStatement = currentStatement.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(finalStatement))
        {
            statements.Add(RemoveTerminator(finalStatement));
        }

        return statements.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
    }

    /// <summary>
    /// Checks if a word at a specific position in the SQL script matches any of the target words
    /// </summary>
    private static bool IsWordAtPosition(string script, int position, string[] targets)
    {
        foreach (var target in targets)
        {
            if (position + target.Length > script.Length
             || !script.Substring(position, target.Length).Equals(target, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Check if the character after the word is whitespace, semicolon, or end of script
            if (position + target.Length == script.Length ||
                char.IsWhiteSpace(script[position + target.Length]) ||
                script[position + target.Length] == Semicolon)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the position is at the start of a line (preceded by newline or nothing)
    /// </summary>
    private static bool IsWordStartOfLine(string script, int position)
    {
        if (position == 0)
        {
            return true;
        }

        for (var i = position - 1; i >= 0; i--)
        {
            var c = script[i];
            if (c is NewLine or CarriageReturn)
            {
                return true;
            }

            if (!char.IsWhiteSpace(c))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Extracts a word starting at the given position
    /// </summary>
    private static string ExtractWord(string script, int position)
    {
        var end = position;
        while (end < script.Length && char.IsLetter(script[end]))
        {
            end++;
        }

        if (end > position)
        {
            return script.Substring(position, end - position).ToUpperInvariant();
        }

        return string.Empty;
    }

    /// <summary>
    /// Checks if a slash at the given position is a statement terminator
    /// (must be alone on a line or followed only by whitespace)
    /// </summary>
    private static bool IsSlashTerminator(string script, int position)
    {
        // Check characters before slash
        var hasNonWhitespaceBefore = false;
        for (var i = position - 1; i >= 0; i--)
        {
            if (script[i] == NewLine || script[i] == CarriageReturn)
            {
                break;
            }

            if (char.IsWhiteSpace(script[i]))
            {
                continue;
            }

            hasNonWhitespaceBefore = true;
            break;
        }

        // Check characters after slash
        var hasNonWhitespaceAfter = false;
        for (var i = position + 1; i < script.Length; i++)
        {
            if (script[i] == NewLine || script[i] == CarriageReturn)
            {
                break;
            }

            if (char.IsWhiteSpace(script[i]))
            {
                continue;
            }

            hasNonWhitespaceAfter = true;
            break;
        }

        return !hasNonWhitespaceBefore && !hasNonWhitespaceAfter;
    }

    /// <summary>
    /// Removes terminator characters from the end of a statement
    /// </summary>
    private static string RemoveTerminator(string statement)
    {
        foreach (var terminator in StatementTerminators)
        {
            if (statement.EndsWith(terminator))
            {
                return statement.Substring(0, statement.Length - terminator.Length).Trim();
            }
        }
        return statement;
    }
}
