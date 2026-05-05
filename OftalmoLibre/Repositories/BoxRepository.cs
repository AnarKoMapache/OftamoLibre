using Microsoft.Data.Sqlite;
using OftalmoLibre.Data;
using OftalmoLibre.Models;

namespace OftalmoLibre.Repositories;

public sealed class BoxRepository
{
    public List<BoxLocation> GetAll(string? search = null, bool includeInactive = true)
    {
        var term = search?.Trim() ?? string.Empty;
        return Database.Query(
            """
            SELECT id, name, is_active
            FROM boxes
            WHERE (@search = '' OR name LIKE @like)
              AND (@include_inactive = 1 OR is_active = 1)
            ORDER BY name;
            """,
            Map,
            new Dictionary<string, object?>
            {
                ["@search"] = term,
                ["@like"] = $"%{term}%",
                ["@include_inactive"] = includeInactive ? 1 : 0
            });
    }

    public List<string> GetActiveNames()
    {
        return Database.Query(
            """
            SELECT name
            FROM boxes
            WHERE is_active = 1
            ORDER BY name;
            """,
            reader => reader.GetString(0));
    }

    public BoxLocation? GetById(int id)
    {
        return Database.QuerySingle(
            """
            SELECT id, name, is_active
            FROM boxes
            WHERE id = @id
            LIMIT 1;
            """,
            Map,
            new Dictionary<string, object?> { ["@id"] = id });
    }

    public BoxLocation? GetByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return Database.QuerySingle(
            """
            SELECT id, name, is_active
            FROM boxes
            WHERE lower(name) = lower(@name)
            LIMIT 1;
            """,
            Map,
            new Dictionary<string, object?> { ["@name"] = name.Trim() });
    }

    public void Save(BoxLocation box, string? previousName = null)
    {
        previousName ??= box.Name;

        if (box.Id == 0)
        {
            box.Id = (int)Database.ExecuteInsert(
                """
                INSERT INTO boxes (name, is_active)
                VALUES (@name, @is_active);
                """,
                new Dictionary<string, object?>
                {
                    ["@name"] = box.Name.Trim(),
                    ["@is_active"] = box.IsActive ? 1 : 0
                });
            return;
        }

        Database.Execute(
            """
            UPDATE boxes
            SET name = @name,
                is_active = @is_active
            WHERE id = @id;
            """,
            new Dictionary<string, object?>
            {
                ["@id"] = box.Id,
                ["@name"] = box.Name.Trim(),
                ["@is_active"] = box.IsActive ? 1 : 0
            });

        if (!string.Equals(previousName, box.Name, StringComparison.OrdinalIgnoreCase))
        {
            Database.Execute(
                """
                UPDATE appointments
                SET agenda = @new_name
                WHERE lower(agenda) = lower(@old_name);
                """,
                new Dictionary<string, object?>
                {
                    ["@new_name"] = box.Name.Trim(),
                    ["@old_name"] = previousName.Trim()
                });
        }
    }

    public void SetActive(int id, bool isActive)
    {
        Database.Execute(
            """
            UPDATE boxes
            SET is_active = @is_active
            WHERE id = @id;
            """,
            new Dictionary<string, object?>
            {
                ["@id"] = id,
                ["@is_active"] = isActive ? 1 : 0
            });
    }

    private static BoxLocation Map(SqliteDataReader reader)
    {
        return new BoxLocation
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            IsActive = reader.GetInt32(2) == 1
        };
    }
}
