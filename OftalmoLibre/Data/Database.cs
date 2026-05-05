using Microsoft.Data.Sqlite;

namespace OftalmoLibre.Data;

public static class Database
{
    private static string ConnectionString => $"Data Source={DbPaths.DatabasePath}";

    public static SqliteConnection OpenConnection()
    {
        DbPaths.EnsureDirectories();

        var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        command.ExecuteNonQuery();

        return connection;
    }

    public static int Execute(string sql, IDictionary<string, object?>? parameters = null)
    {
        using var connection = OpenConnection();
        using var command = CreateCommand(connection, sql, parameters);
        return command.ExecuteNonQuery();
    }

    public static long ExecuteInsert(string sql, IDictionary<string, object?>? parameters = null)
    {
        using var connection = OpenConnection();
        using var command = CreateCommand(connection, sql, parameters);
        command.ExecuteNonQuery();

        using var idCommand = connection.CreateCommand();
        idCommand.CommandText = "SELECT last_insert_rowid();";
        return Convert.ToInt64(idCommand.ExecuteScalar() ?? 0L);
    }

    public static object? Scalar(string sql, IDictionary<string, object?>? parameters = null)
    {
        using var connection = OpenConnection();
        using var command = CreateCommand(connection, sql, parameters);
        return command.ExecuteScalar();
    }

    public static List<T> Query<T>(string sql, Func<SqliteDataReader, T> mapper, IDictionary<string, object?>? parameters = null)
    {
        var items = new List<T>();

        using var connection = OpenConnection();
        using var command = CreateCommand(connection, sql, parameters);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            items.Add(mapper(reader));
        }

        return items;
    }

    public static T? QuerySingle<T>(string sql, Func<SqliteDataReader, T> mapper, IDictionary<string, object?>? parameters = null)
    {
        using var connection = OpenConnection();
        using var command = CreateCommand(connection, sql, parameters);
        using var reader = command.ExecuteReader();

        return reader.Read() ? mapper(reader) : default;
    }

    private static SqliteCommand CreateCommand(SqliteConnection connection, string sql, IDictionary<string, object?>? parameters)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;

        if (parameters is null)
        {
            return command;
        }

        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Key, parameter.Value ?? DBNull.Value);
        }

        return command;
    }
}
