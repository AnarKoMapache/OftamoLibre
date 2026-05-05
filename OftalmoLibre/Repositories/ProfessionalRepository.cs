using Microsoft.Data.Sqlite;
using OftalmoLibre.Data;
using OftalmoLibre.Models;

namespace OftalmoLibre.Repositories;

public sealed class ProfessionalRepository
{
    public List<Professional> GetAll(string? search = null, bool includeInactive = true)
    {
        var term = search?.Trim() ?? string.Empty;
        return Database.Query(
            """
            SELECT id, full_name, professional_type, specialty, registration_number, phone, email, is_active, created_at
            FROM professionals
            WHERE (@search = ''
                   OR full_name LIKE @like
                   OR professional_type LIKE @like
                   OR specialty LIKE @like)
              AND (@include_inactive = 1 OR is_active = 1)
            ORDER BY full_name;
            """,
            Map,
            new Dictionary<string, object?>
            {
                ["@search"] = term,
                ["@like"] = $"%{term}%",
                ["@include_inactive"] = includeInactive ? 1 : 0
            });
    }

    public List<Professional> GetActive()
    {
        return GetAll(includeInactive: false);
    }

    public Professional? GetById(int id)
    {
        return Database.QuerySingle(
            """
            SELECT id, full_name, professional_type, specialty, registration_number, phone, email, is_active, created_at
            FROM professionals
            WHERE id = @id
            LIMIT 1;
            """,
            Map,
            new Dictionary<string, object?> { ["@id"] = id });
    }

    public void Save(Professional professional)
    {
        if (professional.Id == 0)
        {
            professional.Id = (int)Database.ExecuteInsert(
                """
                INSERT INTO professionals (full_name, professional_type, specialty, registration_number, phone, email, is_active, created_at)
                VALUES (@full_name, @professional_type, @specialty, @registration_number, @phone, @email, @is_active, @created_at);
                """,
                ToParameters(professional, false));
            return;
        }

        Database.Execute(
            """
            UPDATE professionals
            SET full_name = @full_name,
                professional_type = @professional_type,
                specialty = @specialty,
                registration_number = @registration_number,
                phone = @phone,
                email = @email,
                is_active = @is_active
            WHERE id = @id;
            """,
            ToParameters(professional, true));
    }

    public void SetActive(int id, bool isActive)
    {
        Database.Execute(
            "UPDATE professionals SET is_active = @is_active WHERE id = @id;",
            new Dictionary<string, object?>
            {
                ["@id"] = id,
                ["@is_active"] = isActive ? 1 : 0
            });
    }

    private static Dictionary<string, object?> ToParameters(Professional professional, bool includeId)
    {
        var values = new Dictionary<string, object?>
        {
            ["@full_name"] = professional.FullName.Trim(),
            ["@professional_type"] = professional.ProfessionalType.Trim(),
            ["@specialty"] = professional.Specialty.Trim(),
            ["@registration_number"] = professional.RegistrationNumber,
            ["@phone"] = professional.Phone,
            ["@email"] = professional.Email,
            ["@is_active"] = professional.IsActive ? 1 : 0,
            ["@created_at"] = professional.CreatedAt.ToString("s")
        };

        if (includeId)
        {
            values["@id"] = professional.Id;
        }

        return values;
    }

    private static Professional Map(SqliteDataReader reader)
    {
        return new Professional
        {
            Id = reader.GetInt32(0),
            FullName = reader.GetString(1),
            ProfessionalType = reader.GetString(2),
            Specialty = reader.GetString(3),
            RegistrationNumber = reader.IsDBNull(4) ? null : reader.GetString(4),
            Phone = reader.IsDBNull(5) ? null : reader.GetString(5),
            Email = reader.IsDBNull(6) ? null : reader.GetString(6),
            IsActive = reader.GetInt32(7) == 1,
            CreatedAt = DateTime.Parse(reader.GetString(8))
        };
    }
}
