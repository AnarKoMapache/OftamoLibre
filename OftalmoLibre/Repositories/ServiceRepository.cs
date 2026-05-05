using Microsoft.Data.Sqlite;
using OftalmoLibre.Data;
using OftalmoLibre.Models;

namespace OftalmoLibre.Repositories;

public sealed class ServiceRepository
{
    public List<OphthalmologyService> GetAll(string? search = null, bool includeInactive = true)
    {
        var term = search?.Trim() ?? string.Empty;

        return Database.Query(
            """
            SELECT id, name, description, price, duration_minutes, is_active
            FROM services
            WHERE (@search = ''
                    OR name LIKE @like
                    OR IFNULL(description, '') LIKE @like)
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

    public List<OphthalmologyService> GetActive()
    {
        return GetAll(includeInactive: false);
    }

    public OphthalmologyService? GetById(int id)
    {
        return Database.QuerySingle(
            """
            SELECT id, name, description, price, duration_minutes, is_active
            FROM services
            WHERE id = @id
            LIMIT 1;
            """,
            Map,
            new Dictionary<string, object?> { ["@id"] = id });
    }

    public void Save(OphthalmologyService service)
    {
        if (service.Id == 0)
        {
            service.Id = (int)Database.ExecuteInsert(
                """
                INSERT INTO services (name, description, price, duration_minutes, is_active)
                VALUES (@name, @description, @price, @duration_minutes, @is_active);
                """,
                ToParameters(service, false));
            return;
        }

        Database.Execute(
            """
            UPDATE services
            SET name = @name,
                description = @description,
                price = @price,
                duration_minutes = @duration_minutes,
                is_active = @is_active
            WHERE id = @id;
            """,
            ToParameters(service, true));
    }

    public void SetActive(int id, bool isActive)
    {
        Database.Execute(
            "UPDATE services SET is_active = @is_active WHERE id = @id;",
            new Dictionary<string, object?>
            {
                ["@id"] = id,
                ["@is_active"] = isActive ? 1 : 0
            });
    }

    private static Dictionary<string, object?> ToParameters(OphthalmologyService service, bool includeId)
    {
        var values = new Dictionary<string, object?>
        {
            ["@name"] = service.Name.Trim(),
            ["@description"] = service.Description,
            ["@price"] = service.Price,
            ["@duration_minutes"] = service.DurationMinutes,
            ["@is_active"] = service.IsActive ? 1 : 0
        };

        if (includeId)
        {
            values["@id"] = service.Id;
        }

        return values;
    }

    private static OphthalmologyService Map(SqliteDataReader reader)
    {
        return new OphthalmologyService
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
            Price = reader.GetDecimal(3),
            DurationMinutes = reader.GetInt32(4),
            IsActive = reader.GetInt32(5) == 1
        };
    }
}
