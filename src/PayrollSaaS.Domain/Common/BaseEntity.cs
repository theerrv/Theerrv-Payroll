namespace PayrollSaaS.Domain.Common;

/// <summary>
/// Every entity in the system uses a Guid PK and records its creation timestamp.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
