using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollSaaS.Application.Interfaces;
using PayrollSaaS.Domain.Enums;
using PayrollSaaS.Infrastructure.Persistence;

namespace PayrollSaaS.API.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize(Policy = "Finance")]
public class ReportsController : ControllerBase
{
    private readonly PayrollDbContext _db;
    private readonly ICurrentUserContext _currentUser;

    public ReportsController(PayrollDbContext db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // ── DTOs ──
    public record PayrollHistoryDto(Guid PayrollRunId, DateOnly PayrollMonth, string DivisionName,
        int LopDays, decimal GrossSalary, decimal NettSalary, decimal EsiAmount);
    public record PfSummaryDto(Guid EmployeeId, string EmployeeName, decimal PfGross,
        decimal EmployerContribution);
    public record EsiSummaryDto(Guid EmployeeId, string EmployeeName, decimal PfGross, decimal EsiAmount);

    /// <summary>GET /reports/employee/{id}/payroll-history?from=&to= — (doc §6, step 12).</summary>
    [HttpGet("employee/{employeeId:guid}/payroll-history")]
    public async Task<ActionResult<List<PayrollHistoryDto>>> PayrollHistory(
        Guid employeeId, [FromQuery] string? from, [FromQuery] string? to)
    {
        var query = _db.PayrollEntries
            .Include(e => e.PayrollRun)
            .Where(e => e.EmployeeId == employeeId && e.PayrollRun.Status == PayrollRunStatus.Finalized);

        if (from is not null && DateOnly.TryParse(from + "-01", out var f))
            query = query.Where(e => e.PayrollRun.PayrollMonth >= f);
        if (to is not null && DateOnly.TryParse(to + "-01", out var t))
            query = query.Where(e => e.PayrollRun.PayrollMonth <= t);

        var results = await query
            .Join(_db.SchoolDivisions, e => e.PayrollRun.SchoolDivisionId, d => d.Id, (e, d) => new { e, d })
            .OrderByDescending(x => x.e.PayrollRun.PayrollMonth)
            .Select(x => new PayrollHistoryDto(x.e.PayrollRunId, x.e.PayrollRun.PayrollMonth,
                x.d.Name, x.e.LopDays, x.e.GrossSalary, x.e.NettSalary, x.e.EsiAmount))
            .ToListAsync();

        return Ok(results);
    }

    /// <summary>GET /reports/pf-summary?month= — Per-employee PF amounts for a finalized month (doc §6).</summary>
    [HttpGet("pf-summary")]
    public async Task<ActionResult<List<PfSummaryDto>>> PfSummary(
        [FromQuery] string month, [FromQuery] Guid? divisionId)
    {
        if (!DateOnly.TryParse(month + "-01", out var payrollMonth))
            return BadRequest(new ProblemDetails { Title = "Invalid month format. Use YYYY-MM." });

        // Get the PF rate from settings
        var settings = await _db.SchoolPayrollSettings.FirstOrDefaultAsync(s => s.SchoolId == _currentUser.SchoolId!.Value);
        var pfRate = settings?.EmployerPfRate ?? 0.12m;

        var query = _db.PayrollEntries
            .Include(e => e.PayrollRun)
            .Where(e => e.PayrollRun.PayrollMonth == payrollMonth
                     && e.PayrollRun.Status == PayrollRunStatus.Finalized
                     && e.IsPfEligible);

        if (divisionId.HasValue)
            query = query.Where(e => e.PayrollRun.SchoolDivisionId == divisionId.Value);

        var results = await query
            .Join(_db.Employees, e => e.EmployeeId, emp => emp.Id, (e, emp) => new { e, emp })
            .OrderBy(x => x.emp.StaffName)
            .Select(x => new PfSummaryDto(x.e.EmployeeId, x.emp.StaffName, x.e.PfGross,
                Math.Round(x.e.PfGross * pfRate, 4)))
            .ToListAsync();

        return Ok(results);
    }

    /// <summary>GET /reports/esi-summary?month= — Per-employee ESI amounts for a finalized month (doc §6).</summary>
    [HttpGet("esi-summary")]
    public async Task<ActionResult<List<EsiSummaryDto>>> EsiSummary(
        [FromQuery] string month, [FromQuery] Guid? divisionId)
    {
        if (!DateOnly.TryParse(month + "-01", out var payrollMonth))
            return BadRequest(new ProblemDetails { Title = "Invalid month format. Use YYYY-MM." });

        var query = _db.PayrollEntries
            .Include(e => e.PayrollRun)
            .Where(e => e.PayrollRun.PayrollMonth == payrollMonth
                     && e.PayrollRun.Status == PayrollRunStatus.Finalized
                     && e.IsPfEligible
                     && e.EsiAmount > 0);

        if (divisionId.HasValue)
            query = query.Where(e => e.PayrollRun.SchoolDivisionId == divisionId.Value);

        var results = await query
            .Join(_db.Employees, e => e.EmployeeId, emp => emp.Id, (e, emp) => new { e, emp })
            .OrderBy(x => x.emp.StaffName)
            .Select(x => new EsiSummaryDto(x.e.EmployeeId, x.emp.StaffName, x.e.PfGross, x.e.EsiAmount))
            .ToListAsync();

        return Ok(results);
    }
}
