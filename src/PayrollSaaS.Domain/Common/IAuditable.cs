namespace PayrollSaaS.Domain.Common;

/// <summary>
/// Marker interface for entities that should be captured in audit_logs by the
/// SaveChangesInterceptor. All mutations (add/update/delete) are logged.
/// </summary>
public interface IAuditable { }
