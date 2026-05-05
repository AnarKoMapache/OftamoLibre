using OftalmoLibre.Data;
using OftalmoLibre.Models;

namespace OftalmoLibre.Repositories;

public sealed class AuditRepository
{
    public void Add(AuditLog log)
    {
        Database.Execute(
            """
            INSERT INTO audit_logs (user_id, action, entity_name, entity_id, details, created_at)
            VALUES (@user_id, @action, @entity_name, @entity_id, @details, @created_at);
            """,
            new Dictionary<string, object?>
            {
                ["@user_id"] = log.UserId,
                ["@action"] = log.Action,
                ["@entity_name"] = log.EntityName,
                ["@entity_id"] = log.EntityId,
                ["@details"] = log.Details,
                ["@created_at"] = log.CreatedAt.ToString("s")
            });
    }

    public List<AuditLog> GetRecent(int limit = 50)
    {
        return Database.Query(
            $"""
            SELECT id, user_id, action, entity_name, entity_id, details, created_at
            FROM audit_logs
            ORDER BY created_at DESC
            LIMIT {limit};
            """,
            reader => new AuditLog
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                Action = reader.GetString(2),
                EntityName = reader.GetString(3),
                EntityId = reader.IsDBNull(4) ? null : reader.GetString(4),
                Details = reader.IsDBNull(5) ? null : reader.GetString(5),
                CreatedAt = DateTime.Parse(reader.GetString(6))
            });
    }
}
