using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollSaaS.Application.Interfaces;
using PayrollSaaS.Domain.Entities.Attendance;
using PayrollSaaS.Domain.Enums;
using PayrollSaaS.Infrastructure.Persistence;

namespace PayrollSaaS.API.Controllers;

[ApiController]
[Route("api/v1/attendance")]
[Authorize(Policy = "Hr")]
public class AttendanceController : ControllerBase
{
    private readonly PayrollDbContext _db;
    private readonly ICurrentUserContext _currentUser;

    public AttendanceController(PayrollDbContext db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public record AttendanceDto(Guid EmployeeId, DateOnly Date, bool IsPresent, Guid EnteredBy);
    public record BulkAttendanceRequest(DateOnly Date, Guid DivisionId, List<EmployeeAttendance> Records);
    public record EmployeeAttendance(Guid EmployeeId, bool IsPresent);
    public record LopSummaryDto(Guid EmployeeId, string StaffName, int LopDays);

    /// <summary>GET /attendance — Monthly attendance (doc §6). Query: ?month=2025-08&amp;employeeId=</summary>
    [HttpGet]
    public async Task<ActionResult<List<AttendanceDto>>> List([FromQuery] string month, [FromQuery] Guid? employeeId)
    {
        if (!DateOnly.TryParse(month + "-01", out var monthStart))
            return BadRequest(new ProblemDetails { Title = "Invalid month format. Use YYYY-MM." });

        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var query = _db.AttendanceRecords
            .Where(a => a.AttendanceDate >= monthStart && a.AttendanceDate <= monthEnd);

        if (employeeId.HasValue)
            query = query.Where(a => a.EmployeeId == employeeId.Value);

        return Ok(await query.OrderBy(a => a.AttendanceDate)
            .Select(a => new AttendanceDto(a.EmployeeId, a.AttendanceDate, a.IsPresent, a.EnteredBy))
            .ToListAsync());
    }

    /// <summary>POST /attendance/bulk — Idempotent upsert for a division + date (doc §6).
    /// Rejects dates inside a finalized run with 409.</summary>
    [HttpPost("bulk")]
    public async Task<IActionResult> BulkUpsert([FromBody] BulkAttendanceRequest request)
    {
        // Check for finalized run covering this date
        var monthStart = new DateOnly(request.Date.Year, request.Date.Month, 1);
        var finalized = await _db.PayrollRuns
            .AnyAsync(r => r.SchoolDivisionId == request.DivisionId
                        && r.PayrollMonth == monthStart
                        && r.Status == PayrollRunStatus.Finalized);
        if (finalized)
            return Conflict(new ProblemDetails { Title = "Cannot modify attendance for a finalized payroll month." });

        foreach (var rec in request.Records)
        {
            var existing = await _db.AttendanceRecords
                .FirstOrDefaultAsync(a => a.EmployeeId == rec.EmployeeId && a.AttendanceDate == request.Date);

            if (existing is not null)
            {
                existing.IsPresent = rec.IsPresent;
                existing.EnteredBy = _currentUser.UserId;
            }
            else
            {
                _db.AttendanceRecords.Add(new AttendanceRecord
                {
                    EmployeeId = rec.EmployeeId,
                    AttendanceDate = request.Date,
                    IsPresent = rec.IsPresent,
                    EnteredBy = _currentUser.UserId
                });
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { Updated = request.Records.Count });
    }

    /// <summary>PUT /attendance/{employeeId}/{date} — Correct a single attendance record (doc §6).</summary>
    [HttpPut("{employeeId:guid}/{date}")]
    public async Task<IActionResult> Correct(Guid employeeId, DateOnly date, [FromBody] bool isPresent)
    {
        var monthStart = new DateOnly(date.Year, date.Month, 1);
        var finalized = await _db.PayrollRuns
            .AnyAsync(r => r.SchoolDivisionId ==
                          _db.Employees.Where(e => e.Id == employeeId).Select(e => e.SchoolDivisionId).FirstOrDefault()
                        && r.PayrollMonth == monthStart
                        && r.Status == PayrollRunStatus.Finalized);
        if (finalized)
            return Conflict(new ProblemDetails { Title = "Cannot modify attendance for a finalized payroll month." });

        var record = await _db.AttendanceRecords
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.AttendanceDate == date);

        if (record is null)
        {
            _db.AttendanceRecords.Add(new AttendanceRecord
            {
                EmployeeId = employeeId, AttendanceDate = date,
                IsPresent = isPresent, EnteredBy = _currentUser.UserId
            });
        }
        else
        {
            record.IsPresent = isPresent;
            record.EnteredBy = _currentUser.UserId;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>GET /attendance/lop-summary — LOP day counts per employee (doc §6). Single grouped query.</summary>
    [HttpGet("lop-summary")]
    public async Task<ActionResult<List<LopSummaryDto>>> LopSummary([FromQuery] string month)
    {
        if (!DateOnly.TryParse(month + "-01", out var monthStart))
            return BadRequest(new ProblemDetails { Title = "Invalid month format. Use YYYY-MM." });

        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var summary = await _db.AttendanceRecords
            .Where(a => a.AttendanceDate >= monthStart && a.AttendanceDate <= monthEnd && !a.IsPresent)
            .GroupBy(a => a.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, LopDays = g.Count() })
            .Join(_db.Employees, l => l.EmployeeId, e => e.Id,
                  (l, e) => new LopSummaryDto(l.EmployeeId, e.StaffName, l.LopDays))
            .ToListAsync();

        return Ok(summary);
    }
}
