using OftalmoLibre.Models;
using OftalmoLibre.Repositories;

namespace OftalmoLibre.Services;

public sealed class AuditService
{
    private readonly AuditRepository _repository = new();

    public void Log(int userId, string action, string entityName, string? entityId = null, string? details = null)
    {
        _repository.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            CreatedAt = DateTime.Now
        });
    }
}
