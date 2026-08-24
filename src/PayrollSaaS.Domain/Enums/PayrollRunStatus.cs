namespace PayrollSaaS.Domain.Enums;

/// <summary>Lifecycle: Draft → Submitted → Approved → Finalized (doc §2).</summary>
public enum PayrollRunStatus { Draft, Submitted, Approved, Finalized }
