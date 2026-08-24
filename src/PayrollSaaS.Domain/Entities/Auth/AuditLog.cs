using PayrollSaaS.Domain.Common;

namespace PayrollSaaS.Domain.Entities.Auth;

/// <summary>
/// Doc §5.6. entity_type, entity_id, action, changes (jsonb) — full change history.
/// Written by the SaveChangesInterceptor, never by application code directly.
/// </summary>
public class AuditLog : BaseEntity
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }

    /// <summary>Added, Modified, or Deleted.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>JSON object capturing old/new values.</summary>
    public string Changes { get; set; } = "{}";

    /// <summary>The user who made the change (null for system/background jobs).</summary>
    public Guid? PerformedBy { get; set; }

    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
}
