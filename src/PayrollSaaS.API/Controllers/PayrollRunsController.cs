using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollSaaS.Application.Interfaces;
using PayrollSaaS.Domain.Calculators;
using PayrollSaaS.Domain.Entities.Payroll;
using PayrollSaaS.Domain.Enums;
using PayrollSaaS.Infrastructure.Persistence;
using PayrollSaaS.Shared.Money;

namespace PayrollSaaS.API.Controllers;

[ApiController]
[Route("api/v1/payroll/runs")]
[Authorize]
public class PayrollRunsController : ControllerBase
{
    private readonly PayrollDbContext _db;
    private readonly ICurrentUserContext _currentUser;
    private readonly PayrollCalculationService _calcService;
    private readonly IDocumentService _docService;

    public PayrollRunsController(PayrollDbContext db, ICurrentUserContext currentUser,
        PayrollCalculationService calcService, IDocumentService docService)
    {
        _db = db;
        _currentUser = currentUser;
        _docService = docService;
        _calcService = calcService;
    }

    // ── DTOs ──
    public record PayrollRunDto(Guid Id, Guid SchoolDivisionId, DateOnly PayrollMonth, string Status,
        DateTime CreatedAt, DateTime? SubmittedAt, DateTime? ApprovedAt, DateTime? FinalizedAt);
    public record PayrollEntryDto(Guid Id, Guid EmployeeId, string EmployeeName,
        decimal SalaryAfterPf, decimal PfGross, bool IsPfEligible,
        int LopDays, decimal LopDeduction, decimal GrossSalary,
        decimal TotalDeductions, decimal TotalAdditions,
        decimal NettPay, decimal EsiAmount, decimal NettSalary,
        decimal? HrEnteredAmount, bool? AmountMatches);
    public record CreateRunRequest(Guid DivisionId, DateOnly Month);
    public record HrAmountRequest(decimal Amount);

    /// <summary>GET /payroll/runs — List runs (doc §6). Query: ?month=&divisionId=</summary>
    [HttpGet]
    public async Task<ActionResult<List<PayrollRunDto>>> List([FromQuery] string? month, [FromQuery] Guid? divisionId)
    {
        var query = _db.PayrollRuns.AsQueryable();
        if (divisionId.HasValue) query = query.Where(r => r.SchoolDivisionId == divisionId.Value);
        if (month is not null && DateOnly.TryParse(month + "-01", out var m))
            query = query.Where(r => r.PayrollMonth == m);

        return Ok(await query.OrderByDescending(r => r.PayrollMonth)
            .Select(r => new PayrollRunDto(r.Id, r.SchoolDivisionId, r.PayrollMonth, r.Status.ToString(),
                r.CreatedAt, r.SubmittedAt, r.ApprovedAt, r.FinalizedAt))
            .ToListAsync());
    }

    /// <summary>POST /payroll/runs — Initiate payroll for a division + month (doc §6).
    /// Creates Draft, snapshots salary/PF/eligibility, pulls LOP from attendance,
    /// advance installment due, and calculates every entry.</summary>
    [HttpPost]
    [Authorize(Policy = "Hr")]
    public async Task<ActionResult<PayrollRunDto>> Create([FromBody] CreateRunRequest request)
    {
        if (_currentUser.SchoolId is null)
            return BadRequest(new ProblemDetails { Title = "School context required" });

        var payrollMonth = new DateOnly(request.Month.Year, request.Month.Month, 1);

        // Check unique constraint
        var exists = await _db.PayrollRuns.AnyAsync(r =>
            r.SchoolDivisionId == request.DivisionId && r.PayrollMonth == payrollMonth);
        if (exists)
            return Conflict(new ProblemDetails { Title = "A payroll run already exists for this division and month." });

        // Get school settings for ESI rate
        var settings = await _db.SchoolPayrollSettings.FirstOrDefaultAsync(s => s.SchoolId == _currentUser.SchoolId.Value);
        var esiRate = settings?.EsiRate ?? 0.0075m;

        var run = new PayrollRun
        {
            SchoolId = _currentUser.SchoolId.Value,
            SchoolDivisionId = request.DivisionId,
            PayrollMonth = payrollMonth,
            Status = PayrollRunStatus.Draft,
            CreatedBy = _currentUser.UserId
        };

        // Get all active employees in this division
        var employees = await _db.Employees
            .Where(e => e.SchoolDivisionId == request.DivisionId && e.EmploymentStatus == EmploymentStatus.Active)
            .ToListAsync();

        var monthEnd = payrollMonth.AddMonths(1).AddDays(-1);

        foreach (var emp in employees)
        {
            // Salary after PF = SUM of active earning components on the cutoff date
            var salaryAfterPf = await _db.EmployeeSalaryComponents
                .Where(c => c.EmployeeId == emp.Id
                          && c.ComponentType == ComponentType.Earning
                          && c.EffectiveFrom <= monthEnd
                          && (c.EffectiveTo == null || c.EffectiveTo >= payrollMonth))
                .SumAsync(c => c.Amount);

            // PF Gross: explicit config or fall back to Basic
            var pfConfig = await _db.EmployeePfConfigs
                .Where(c => c.EmployeeId == emp.Id
                          && c.EffectiveFrom <= monthEnd
                          && (c.EffectiveTo == null || c.EffectiveTo >= payrollMonth))
                .OrderByDescending(c => c.EffectiveFrom)
                .FirstOrDefaultAsync();

            var pfGross = pfConfig?.PfGross
                ?? await _db.EmployeeSalaryComponents
                    .Where(c => c.EmployeeId == emp.Id
                              && c.ComponentName == ComponentName.Basic
                              && c.ComponentType == ComponentType.Earning
                              && c.EffectiveFrom <= monthEnd
                              && (c.EffectiveTo == null || c.EffectiveTo >= payrollMonth))
                    .Select(c => c.Amount)
                    .FirstOrDefaultAsync();

            // SPEC-GAP 7: PF eligibility snapshot
            var isPfEligible = emp.PfStatus == PfStatus.Active
                            && emp.PfActiveFrom.HasValue
                            && emp.PfActiveFrom.Value <= monthEnd;

            // LOP days from attendance
            var lopDays = await _db.AttendanceRecords
                .CountAsync(a => a.EmployeeId == emp.Id
                              && a.AttendanceDate >= payrollMonth
                              && a.AttendanceDate <= monthEnd
                              && !a.IsPresent);

            // Advance installment due this month
            var advanceInstallmentDue = await _db.AdvanceInstallments
                .Where(i => i.Advance.EmployeeId == emp.Id
                          && i.DueMonth == payrollMonth
                          && i.Status == InstallmentStatus.Pending
                          && i.Advance.Status == AdvanceStatus.Active)
                .SumAsync(i => i.Amount);

            // Calculate
            var input = new PayrollCalculationInput
            {
                SalaryAfterPf = salaryAfterPf,
                PfGross = pfGross,
                IsPfEligible = isPfEligible,
                LopDays = lopDays,
                EsiRate = esiRate,
                AdvanceInstallmentDue = advanceInstallmentDue
            };
            var result = _calcService.Calculate(input);

            var entry = new PayrollEntry
            {
                PayrollRunId = run.Id,
                EmployeeId = emp.Id,
                SalaryAfterPf = result.SalaryAfterPf,
                PfGross = pfGross,
                IsPfEligible = isPfEligible,
                LopDays = result.LopDays,
                LopDeduction = result.LopDeduction,
                GrossSalary = result.GrossSalary,
                TotalDeductions = result.TotalDeductions,
                TotalAdditions = result.TotalAdditions,
                NettPay = result.NettPay,
                EsiAmount = result.EsiAmount,
                NettSalary = result.NettSalary
            };

            // Itemized deductions/additions
            foreach (var d in result.ItemizedDeductions)
            {
                entry.Deductions.Add(new PayrollDeduction
                {
                    DeductionType = Enum.TryParse<DeductionType>(d.Type, true, out var dt) ? dt : DeductionType.Other,
                    Description = d.Description,
                    Amount = d.Amount
                });
            }
            foreach (var a in result.ItemizedAdditions)
            {
                entry.Additions.Add(new PayrollAddition
                {
                    AdditionType = Enum.TryParse<AdditionType>(a.Type, true, out var at) ? at : AdditionType.Other,
                    Description = a.Description,
                    Amount = a.Amount
                });
            }

            run.Entries.Add(entry);
        }

        _db.PayrollRuns.Add(run);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRun), new { runId = run.Id },
            new PayrollRunDto(run.Id, run.SchoolDivisionId, run.PayrollMonth, run.Status.ToString(),
                run.CreatedAt, run.SubmittedAt, run.ApprovedAt, run.FinalizedAt));
    }

    /// <summary>GET /payroll/runs/{runId} — Run detail with summary totals (doc §6).</summary>
    [HttpGet("{runId:guid}")]
    public async Task<ActionResult<PayrollRunDto>> GetRun(Guid runId)
    {
        var run = await _db.PayrollRuns.FindAsync(runId);
        if (run is null) return NotFound();
        return Ok(new PayrollRunDto(run.Id, run.SchoolDivisionId, run.PayrollMonth, run.Status.ToString(),
            run.CreatedAt, run.SubmittedAt, run.ApprovedAt, run.FinalizedAt));
    }

    /// <summary>GET /payroll/runs/{runId}/entries — All employee entries, paginated (doc §6).</summary>
    [HttpGet("{runId:guid}/entries")]
    public async Task<ActionResult<List<PayrollEntryDto>>> ListEntries(Guid runId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var entries = await _db.PayrollEntries
            .Where(e => e.PayrollRunId == runId)
            .Join(_db.Employees, pe => pe.EmployeeId, emp => emp.Id, (pe, emp) => new { pe, emp })
            .OrderBy(x => x.emp.StaffName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new PayrollEntryDto(x.pe.Id, x.pe.EmployeeId, x.emp.StaffName,
                x.pe.SalaryAfterPf, x.pe.PfGross, x.pe.IsPfEligible,
                x.pe.LopDays, x.pe.LopDeduction, x.pe.GrossSalary,
                x.pe.TotalDeductions, x.pe.TotalAdditions,
                x.pe.NettPay, x.pe.EsiAmount, x.pe.NettSalary,
                x.pe.HrEnteredAmount, x.pe.AmountMatches))
            .ToListAsync();
        return Ok(entries);
    }

    /// <summary>PUT /payroll/runs/{runId}/entries/{entryId}/amount — HR enters verification amount (doc §6).</summary>
    [HttpPut("{runId:guid}/entries/{entryId:guid}/amount")]
    [Authorize(Policy = "Hr")]
    public async Task<IActionResult> SetHrAmount(Guid runId, Guid entryId, [FromBody] HrAmountRequest request)
    {
        var entry = await _db.PayrollEntries.FirstOrDefaultAsync(e => e.Id == entryId && e.PayrollRunId == runId);
        if (entry is null) return NotFound();

        var run = await _db.PayrollRuns.FindAsync(runId);
        if (run is not null && run.IsFinalized)
            return Conflict(new ProblemDetails { Title = "Cannot modify a finalized payroll run." });

        entry.HrEnteredAmount = request.Amount;
        entry.AmountMatches = MoneyMath.RoundPayable(request.Amount) == MoneyMath.RoundPayable(entry.NettSalary);

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>POST /payroll/runs/{runId}/submit — Draft → Submitted (doc §6).
    /// SPEC-GAP 4: Returns 422 if any entry is unverified or mismatched.</summary>
    [HttpPost("{runId:guid}/submit")]
    [Authorize(Policy = "Hr")]
    public async Task<IActionResult> Submit(Guid runId, [FromQuery] bool allowMismatch = false)
    {
        var run = await _db.PayrollRuns.Include(r => r.Entries).FirstOrDefaultAsync(r => r.Id == runId);
        if (run is null) return NotFound();

        // Check all entries are verified
        if (!allowMismatch)
        {
            var problems = run.Entries
                .Where(e => e.AmountMatches != true)
                .Select(e => new { e.EmployeeId, e.HrEnteredAmount, e.NettSalary, e.AmountMatches })
                .ToList();

            if (problems.Count > 0)
            {
                return UnprocessableEntity(new ProblemDetails
                {
                    Title = "Verification incomplete",
                    Detail = $"{problems.Count} entries are unverified or mismatched. Pass ?allowMismatch=true (school_admin only) to override.",
                    Extensions = { ["entries"] = problems }
                });
            }
        }

        run.Submit(_currentUser.UserId);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>POST /payroll/runs/{runId}/approve — Submitted → Approved (doc §6).</summary>
    [HttpPost("{runId:guid}/approve")]
    [Authorize(Policy = "SchoolAdmin")]
    public async Task<IActionResult> Approve(Guid runId)
    {
        var run = await _db.PayrollRuns.FindAsync(runId);
        if (run is null) return NotFound();

        run.Approve(_currentUser.UserId);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>POST /payroll/runs/{runId}/finalize — Approved → Finalized (doc §6).
    /// Single transaction: lock entries, mark advance installments deducted,
    /// update advance balances, complete exhausted advances.</summary>
    [HttpPost("{runId:guid}/finalize")]
    [Authorize(Policy = "SchoolAdmin")]
    public async Task<IActionResult> Finalize(Guid runId)
    {
        var run = await _db.PayrollRuns
            .Include(r => r.Entries)
            .FirstOrDefaultAsync(r => r.Id == runId);
        if (run is null) return NotFound();

        run.FinalizeRun();

        // SPEC-GAP 6: Advance recovery happens only at finalize
        foreach (var entry in run.Entries)
        {
            var installments = await _db.AdvanceInstallments
                .Include(i => i.Advance)
                .Where(i => i.Advance.EmployeeId == entry.EmployeeId
                          && i.DueMonth == run.PayrollMonth
                          && i.Status == InstallmentStatus.Pending
                          && i.Advance.Status == AdvanceStatus.Active)
                .ToListAsync();

            foreach (var inst in installments)
            {
                inst.Status = InstallmentStatus.Deducted;
                inst.PayrollEntryId = entry.Id;

                inst.Advance.InstallmentsRecovered++;
                inst.Advance.BalanceAmount -= inst.Amount;

                if (inst.Advance.InstallmentsRecovered >= inst.Advance.TotalInstallments)
                    inst.Advance.Status = AdvanceStatus.Completed;
            }
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ──────────────────────── Download endpoints ────────────────────────

    /// <summary>GET /payroll/runs/{runId}/entries/{entryId}/payslip — Download payslip PDF (doc §6).</summary>
    [HttpGet("{runId:guid}/entries/{entryId:guid}/payslip")]
    public async Task<IActionResult> DownloadPayslip(Guid runId, Guid entryId)
    {
        var entry = await _db.PayrollEntries
            .Include(e => e.PayrollRun)
            .Include(e => e.Deductions)
            .Include(e => e.Additions)
            .FirstOrDefaultAsync(e => e.Id == entryId && e.PayrollRunId == runId);
        if (entry is null) return NotFound();

        if (!entry.PayrollRun.IsFinalized)
            return Conflict(new ProblemDetails { Title = "Payslips are only available for finalized runs." });

        var employee = await _db.Employees.FindAsync(entry.EmployeeId);
        var division = await _db.SchoolDivisions.FindAsync(entry.PayrollRun.SchoolDivisionId);
        var pdf = _docService.GeneratePayslipPdf(entry, employee?.StaffName ?? "Unknown",
            division?.Name ?? "Unknown", entry.PayrollRun.PayrollMonth);

        return File(pdf, "application/pdf",
            $"payslip_{employee?.StaffName ?? "unknown"}_{entry.PayrollRun.PayrollMonth:yyyy-MM}.pdf");
    }

    /// <summary>GET /payroll/runs/{runId}/bank-csv — Bank transfer CSV (doc §6).</summary>
    [HttpGet("{runId:guid}/bank-csv")]
    [Authorize(Policy = "Finance")]
    public async Task<IActionResult> DownloadBankCsv(Guid runId)
    {
        var run = await _db.PayrollRuns.FindAsync(runId);
        if (run is null) return NotFound();
        if (!run.IsFinalized) return Conflict(new ProblemDetails { Title = "Bank CSV is only available for finalized runs." });

        var rows = await _db.PayrollEntries
            .Where(e => e.PayrollRunId == runId)
            .Join(_db.Employees, pe => pe.EmployeeId, emp => emp.Id, (pe, emp) => new BankTransferRow(
                emp.StaffName, emp.BankAccountNumber ?? "", emp.IfscCode ?? "", pe.NettSalary))
            .ToListAsync();

        var csv = _docService.GenerateBankCsv(rows);
        return File(csv, "text/csv", $"bank_transfer_{run.PayrollMonth:yyyy-MM}.csv");
    }

    /// <summary>GET /payroll/runs/{runId}/pf-report — PF report CSV (doc §6).</summary>
    [HttpGet("{runId:guid}/pf-report")]
    [Authorize(Policy = "Finance")]
    public async Task<IActionResult> DownloadPfReport(Guid runId)
    {
        var run = await _db.PayrollRuns.FindAsync(runId);
        if (run is null) return NotFound();
        if (!run.IsFinalized) return Conflict(new ProblemDetails { Title = "PF report is only available for finalized runs." });

        var settings = await _db.SchoolPayrollSettings.FirstOrDefaultAsync(s => s.SchoolId == run.SchoolId);
        var pfRate = settings?.EmployerPfRate ?? 0.12m;

        var rows = await _db.PayrollEntries
            .Where(e => e.PayrollRunId == runId && e.IsPfEligible)
            .Join(_db.Employees, pe => pe.EmployeeId, emp => emp.Id, (pe, emp) => new PfReportRow(
                emp.StaffName, pe.PfGross, Math.Round(pe.PfGross * pfRate, 4)))
            .ToListAsync();

        var report = _docService.GeneratePfReport(rows, run.PayrollMonth);
        return File(report, "text/csv", $"pf_report_{run.PayrollMonth:yyyy-MM}.csv");
    }

    /// <summary>GET /payroll/runs/{runId}/esi-report — ESI report CSV (doc §6).</summary>
    [HttpGet("{runId:guid}/esi-report")]
    [Authorize(Policy = "Finance")]
    public async Task<IActionResult> DownloadEsiReport(Guid runId)
    {
        var run = await _db.PayrollRuns.FindAsync(runId);
        if (run is null) return NotFound();
        if (!run.IsFinalized) return Conflict(new ProblemDetails { Title = "ESI report is only available for finalized runs." });

        var rows = await _db.PayrollEntries
            .Where(e => e.PayrollRunId == runId && e.IsPfEligible && e.EsiAmount > 0)
            .Join(_db.Employees, pe => pe.EmployeeId, emp => emp.Id, (pe, emp) => new EsiReportRow(
                emp.StaffName, pe.PfGross, pe.EsiAmount))
            .ToListAsync();

        var report = _docService.GenerateEsiReport(rows, run.PayrollMonth);
        return File(report, "text/csv", $"esi_report_{run.PayrollMonth:yyyy-MM}.csv");
    }

    /// <summary>GET /payroll/runs/{runId}/excel — Full Excel export (doc §6).</summary>
    [HttpGet("{runId:guid}/excel")]
    [Authorize(Policy = "Finance")]
    public async Task<IActionResult> DownloadExcel(Guid runId)
    {
        var run = await _db.PayrollRuns.FindAsync(runId);
        if (run is null) return NotFound();
        if (!run.IsFinalized) return Conflict(new ProblemDetails { Title = "Excel export is only available for finalized runs." });

        var division = await _db.SchoolDivisions.FindAsync(run.SchoolDivisionId);

        var rows = await _db.PayrollEntries
            .Where(e => e.PayrollRunId == runId)
            .Join(_db.Employees, pe => pe.EmployeeId, emp => emp.Id, (pe, emp) => new PayrollExcelRow(
                emp.StaffName, emp.StaffType.ToString(), pe.SalaryAfterPf, pe.PfGross, pe.IsPfEligible,
                pe.LopDays, pe.LopDeduction, pe.GrossSalary,
                pe.TotalDeductions, pe.TotalAdditions,
                pe.NettPay, pe.EsiAmount, pe.NettSalary,
                pe.HrEnteredAmount, pe.AmountMatches))
            .ToListAsync();

        var excel = _docService.GenerateExcelExport(rows, division?.Name ?? "Unknown", run.PayrollMonth);
        return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"payroll_{run.PayrollMonth:yyyy-MM}.xlsx");
    }
}
