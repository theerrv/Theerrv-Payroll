namespace PayrollSaaS.Domain.Enums;

/// <summary>
/// PF lifecycle (doc §5.2):
///   NotEligible → EligiblePendingConfirmation (auto by Hangfire at 1-year anniversary)
///   EligiblePendingConfirmation → Active (HR confirms via PUT /employees/{id}/pf-config)
///   Any → OptedOut (employee opts out)
/// </summary>
public enum PfStatus { NotEligible, EligiblePendingConfirmation, Active, OptedOut }
