using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GoHardAPI.Data
{
    /// <summary>
    /// Recognises a database unique-constraint violation for one SPECIFIC index,
    /// across every provider this project runs on (PostgreSQL in production,
    /// SQL Server locally, SQLite/EF-InMemory in tests). Deliberately narrow: it
    /// answers "was THIS index violated?", never "was any DbUpdateException a
    /// duplicate?", so a caller can map exactly one conflict to a 409 and let
    /// every other <see cref="DbUpdateException"/> propagate unchanged.
    /// </summary>
    internal static class UniqueConstraintViolation
    {
        /// <summary>
        /// True when <paramref name="exception"/> (or any exception in its inner
        /// chain) is a unique-constraint violation attributable to the index named
        /// <paramref name="indexName"/>.
        ///
        /// Matching per provider:
        /// <list type="bullet">
        ///   <item>PostgreSQL: SQLSTATE <c>23505</c> AND
        ///     <see cref="PostgresException.ConstraintName"/> equal to
        ///     <paramref name="indexName"/>.</item>
        ///   <item>SQL Server: error number <c>2601</c>/<c>2627</c> AND the message
        ///     naming <paramref name="indexName"/> (SQL Server puts the index name
        ///     in the violation text).</item>
        ///   <item>SQLite (tests only): message contains
        ///     <c>UNIQUE constraint failed</c> AND names the qualified column,
        ///     supplied via <paramref name="sqliteQualifiedColumns"/> (e.g.
        ///     <c>Users.Username</c>), since SQLite reports table.column, not the
        ///     index name.</item>
        /// </list>
        /// The SQLite arm is matched by type name + message so this file (and the
        /// API assembly) takes no compile-time dependency on Microsoft.Data.Sqlite.
        /// </summary>
        public static bool Matches(
            Exception? exception,
            string indexName,
            params string[] sqliteQualifiedColumns)
        {
            for (var e = exception; e is not null; e = e.InnerException)
            {
                switch (e)
                {
                    case PostgresException pg
                        when pg.SqlState == "23505"
                        && string.Equals(pg.ConstraintName, indexName, StringComparison.Ordinal):
                        return true;

                    // SQL Server quotes the index name in the violation text
                    // ("... with unique index 'IX_Users_Username'."). Match the
                    // quoted form so a longer index sharing this prefix cannot
                    // be mistaken for it.
                    case SqlException sql
                        when SqlNumbers(sql).Any(n => n is 2601 or 2627)
                        && sql.Message.Contains($"'{indexName}'", StringComparison.Ordinal):
                        return true;
                }

                if (e.GetType().FullName == "Microsoft.Data.Sqlite.SqliteException"
                    && e.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase)
                    && sqliteQualifiedColumns.Any(c =>
                        e.Message.Contains(c, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<int> SqlNumbers(SqlException ex)
        {
            yield return ex.Number;
            foreach (SqlError error in ex.Errors)
            {
                yield return error.Number;
            }
        }
    }
}
