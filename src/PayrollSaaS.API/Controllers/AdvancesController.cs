using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollSaaS.Application.Interfaces;
using PayrollSaaS.Domain.Entities.Advances;
using PayrollSaaS.Domain.Enums;
using PayrollSaaS.Infrastructure.Persistence;
using PayrollSaaS.Shared.Money;

namespace PayrollSaaS.API.Controllers;

[ApiController]
[Route("api/v1/advances")]
[Authorize(Policy = "Hr")]
public class AdvancesController : ControllerBase
{
    private readonly PayrollDbContext _db;
    private readonly ICurrentUserContext _currentUser;

    public AdvancesController(PayrollDbContext db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public record AdvanceDto(Guid Id, Guid EmployeeId, decimal TotalAmount, string? Reason,
        DateOnly GivenDate, DateOnly RecoveryStartMonth, decimal InstallmentAmount,
        int TotalInstallments, int InstallmentsRecovered, decimal BalanceAmount, string Status);
    public record InstallmentDto(Guid Id, DateOnly DueMonth, decimal Amount, string Status, Guid? PayrollEntryId);
    public record CreateAdvanceRequest(Guid EmployeeId, decimal TotalAmount, string? Reason,
        DateOnly GivenDate, DateOnly RecoveryStartMonth, decimal InstallmentAmount, int TotalInstallments);

    /// <summary>GET /advances — List advances with filters (doc §6).</summary>
    [HttpGet]
    public async Task<ActionResult<List<AdvanceDto>>> List([FromQuery] Guid? employeeId, [FromQuery] AdvanceStatus? status)
    {
        var query = _db.Advances.AsQueryable();
        if (employeeId.HasValue) query = query.Where(a => a.EmployeeId == employeeId.Value);
        if (status.HasValue) query = query.Where(a => a.Status == status.Value);

        return Ok(await query.OrderByDescending(a => a.GivenDate)
            .Select(a => new AdvanceDto(a.Id, a.EmployeeId, a.TotalAmount, a.Reason,
                a.GivenDate, a.RecoveryStartMonth, a.InstallmentAmount,
                a.TotalInstallments, a.InstallmentsRecovered, a.BalanceAmount, a.Status.ToString()))
            .ToListAsync());
    }

    /// <summary>POST /advances — Create advance with full installment schedule (doc §6).
    /// Final installment absorbs the rounding remainder so the schedule sums exactly to total_amount.</summary>
    [HttpPost]
    public async Task<ActionResult<AdvanceDto>> Create([FromBody] CreateAdvanceRequest request)
    {
        if (_currentUser.SchoolId is null) return BadRequest(new ProblemDetails { Title = "School context required" });

        var advance = new Advance
        {
            EmployeeId = request.EmployeeId,
            SchoolId = _currentUser.SchoolId.Value,
            TotalAmount = request.TotalAmount,
            Reason = request.Reason,
            GivenDate = request.GivenDate,
            RecoveryStartMonth = request.RecoveryStartMonth,
            InstallmentAmount = request.InstallmentAmount,
            TotalInstallments = request.TotalInstallments,
            BalanceAmount = request.TotalAmount,
            Status = AdvanceStatus.Active
        };

        // Generate installment schedule
        var runningTotal = 0m;
        for (int i = 0; i < request.TotalInstallments; i++)
        {
            var dueMonth = request.RecoveryStartMonth.AddMonths(i);
            var isLast = i == request.TotalInstallments - 1;

            // Final installment absorbs rounding remainder
            var amount = isLast
                ? request.TotalAmount - runningTotal
                : MoneyMath.Round(request.InstallmentAmount);

            advance.Installments.Add(new AdvanceInstallment
            {
                AdvanceId = advance.Id,
                DueMonth = dueMonth,
                Amount = amount,
                Status = InstallmentStatus.Pending
            });
            runningTotal += amount;
        }

        _db.Advances.Add(advance);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = advance.Id },
            new AdvanceDto(advance.Id, advance.EmployeeId, advance.TotalAmount, advance.Reason,
                advance.GivenDate, advance.RecoveryStartMonth, advance.InstallmentAmount,
                advance.TotalInstallments, advance.InstallmentsRecovered, advance.BalanceAmount, advance.Status.ToString()));
    }

    /// <summary>GET /advances/{id} — Advance detail (doc §6).</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdvanceDto>> Get(Guid id)
    {
        var a = await _db.Advances.FindAsync(id);
        if (a is null) return NotFound();
        return Ok(new AdvanceDto(a.Id, a.EmployeeId, a.TotalAmount, a.Reason,
            a.GivenDate, a.RecoveryStartMonth, a.InstallmentAmount,
            a.TotalInstallments, a.InstallmentsRecovered, a.BalanceAmount, a.Status.ToString()));
    }

    /// <summary>GET /advances/{id}/installments — Full installment schedule (doc §6).</summary>
    [HttpGet("{id:guid}/installments")]
    public async Task<ActionResult<List<InstallmentDto>>> Installments(Guid id)
    {
        var installments = await _db.AdvanceInstallments
            .Where(i => i.AdvanceId == id)
            .OrderBy(i => i.DueMonth)
            .Select(i => new InstallmentDto(i.Id, i.DueMonth, i.Amount, i.Status.ToString(), i.PayrollEntryId))
            .ToListAsync();
        return Ok(installments);
    }

    /// <summary>PATCH /advances/{id}/cancel — Cancel an active advance (doc §6).
    /// Allowed only while status = active; leaves already-deducted installments intact.</summary>
    [HttpPatch("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var advance = await _db.Advances.Include(a => a.Installments).FirstOrDefaultAsync(a => a.Id == id);
        if (advance is null) return NotFound();
        if (advance.Status != AdvanceStatus.Active)
            return Conflict(new ProblemDetails { Title = $"Cannot cancel: advance is {advance.Status}." });

        advance.Status = AdvanceStatus.Cancelled;
        // Skip all pending installments
        foreach (var inst in advance.Installments.Where(i => i.Status == InstallmentStatus.Pending))
            inst.Status = InstallmentStatus.Skipped;

        await _db.SaveChangesAsync();
        return NoContent();
    }
}
