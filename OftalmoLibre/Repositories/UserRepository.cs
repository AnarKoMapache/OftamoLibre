using Microsoft.Data.Sqlite;
using OftalmoLibre.Data;
using OftalmoLibre.Models;

namespace OftalmoLibre.Repositories;

public sealed class UserRepository
{
    public bool AnyUsers()
    {
        return Convert.ToInt64(Database.Scalar("SELECT COUNT(*) FROM users;") ?? 0L) > 0;
    }

    public void CreateInitialAdministrator(string username, string fullName, string passwordHash)
    {
        Database.Execute(
            """
            INSERT INTO users (username, full_name, password_hash, role, is_active, must_change_password, created_at)
            VALUES (@username, @full_name, @password_hash, @role, 1, 1, @created_at);
            """,
            new Dictionary<string, object?>
            {
                ["@username"] = username,
                ["@full_name"] = fullName,
                ["@password_hash"] = passwordHash,
                ["@role"] = "Administrador",
                ["@created_at"] = DateTime.Now.ToString("s")
            });
    }

    public User? GetByUsername(string username)
    {
        return Database.QuerySingle(
            """
            SELECT id, username, full_name, password_hash, role, is_active, must_change_password, created_at, updated_at
            FROM users
            WHERE lower(username) = lower(@username)
            LIMIT 1;
            """,
            Map,
            new Dictionary<string, object?> { ["@username"] = username.Trim() });
    }

    public User? GetById(int id)
    {
        return Database.QuerySingle(
            """
            SELECT id, username, full_name, password_hash, role, is_active, must_change_password, created_at, updated_at
            FROM users
            WHERE id = @id
            LIMIT 1;
            """,
            Map,
            new Dictionary<string, object?> { ["@id"] = id });
    }

    public List<User> GetAll(string? search = null)
    {
        var term = search?.Trim() ?? string.Empty;

        return Database.Query(
            """
            SELECT id, username, full_name, password_hash, role, is_active, must_change_password, created_at, updated_at
            FROM users
            WHERE @search = ''
               OR username LIKE @like
               OR full_name LIKE @like
               OR role LIKE @like
            ORDER BY full_name;
            """,
            Map,
            new Dictionary<string, object?>
            {
                ["@search"] = term,
                ["@like"] = $"%{term}%"
            });
    }

    public void Save(User user)
    {
        if (user.Id == 0)
        {
            user.Id = (int)Database.ExecuteInsert(
                """
                INSERT INTO users (username, full_name, password_hash, role, is_active, must_change_password, created_at)
                VALUES (@username, @full_name, @password_hash, @role, @is_active, @must_change_password, @created_at);
                """,
                ToParameters(user, false));
            return;
        }

        Database.Execute(
            """
            UPDATE users
            SET username = @username,
                full_name = @full_name,
                password_hash = @password_hash,
                role = @role,
                is_active = @is_active,
                must_change_password = @must_change_password,
                updated_at = @updated_at
            WHERE id = @id;
            """,
            ToParameters(user, true));
    }

    private static Dictionary<string, object?> ToParameters(User user, bool includeId)
    {
        var values = new Dictionary<string, object?>
        {
            ["@username"] = user.Username.Trim(),
            ["@full_name"] = user.FullName.Trim(),
            ["@password_hash"] = user.PasswordHash,
            ["@role"] = user.Role,
            ["@is_active"] = user.IsActive ? 1 : 0,
            ["@must_change_password"] = user.MustChangePassword ? 1 : 0,
            ["@created_at"] = user.CreatedAt.ToString("s"),
            ["@updated_at"] = DateTime.Now.ToString("s")
        };

        if (includeId)
        {
            values["@id"] = user.Id;
        }

        return values;
    }

    private static User Map(SqliteDataReader reader)
    {
        return new User
        {
            Id = reader.GetInt32(0),
            Username = reader.GetString(1),
            FullName = reader.GetString(2),
            PasswordHash = reader.GetString(3),
            Role = reader.GetString(4),
            IsActive = reader.GetInt32(5) == 1,
            MustChangePassword = reader.GetInt32(6) == 1,
            CreatedAt = DateTime.Parse(reader.GetString(7)),
            UpdatedAt = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8))
        };
    }
}
