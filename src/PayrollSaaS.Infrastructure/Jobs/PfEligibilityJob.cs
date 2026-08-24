using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PayrollSaaS.Domain.Enums;
using PayrollSaaS.Infrastructure.Persistence;

namespace PayrollSaaS.Infrastructure.Jobs;

/// <summary>
/// Daily Hangfire job (doc §6, step 11). Flags employees who have reached 1 year of service
/// AND have opted in to PF as eligible_pending_confirmation.
/// HR must still confirm (PUT /employees/{id}/pf-config) — the job never activates PF directly.
/// </summary>
public sealed class PfEligibilityJob
{
    private readonly PayrollDbContext _db;
    private readonly ILogger<PfEligibilityJob> _logger;

    public PfEligibilityJob(PayrollDbContext db, ILogger<PfEligibilityJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var oneYearAgo = today.AddYears(-1);

        // Find employees who should move to EligiblePendingConfirmation
        var candidates = await _db.Employees
            .IgnoreQueryFilters() // Run across all tenants/schools
            .Where(e => e.EmploymentStatus == EmploymentStatus.Active
                     && e.PfOptedIn
                     && e.PfStatus == PfStatus.NotEligible
                     && e.DateOfJoining <= oneYearAgo)
            .ToListAsync();

        if (candidates.Count == 0)
        {
            _logger.LogInformation("PfEligibilityJob: no candidates today ({Date})", today);
            return;
        }

        foreach (var emp in candidates)
            emp.PfStatus = PfStatus.EligiblePendingConfirmation;

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "PfEligibilityJob: flagged {Count} employees as EligiblePendingConfirmation on {Date}",
            candidates.Count, today);
    }
}
