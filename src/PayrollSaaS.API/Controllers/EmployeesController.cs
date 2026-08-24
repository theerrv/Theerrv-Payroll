using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollSaaS.Application.Interfaces;
using PayrollSaaS.Domain.Entities.Employees;
using PayrollSaaS.Domain.Enums;
using PayrollSaaS.Infrastructure.Persistence;

namespace PayrollSaaS.API.Controllers;

[ApiController]
[Route("api/v1/employees")]
[Authorize(Policy = "Hr")]
public class EmployeesController : ControllerBase
{
    private readonly PayrollDbContext _db;
    private readonly ICurrentUserContext _currentUser;

    public EmployeesController(PayrollDbContext db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // ── DTOs ──
    public record EmployeeListDto(Guid Id, string StaffName, string StaffType, string EmploymentStatus, string PfStatus, Guid SchoolDivisionId);
    public record EmployeeDetailDto(Guid Id, string StaffName, string? ContactNumber, string? Email, string StaffType,
        string? BankAccountNumber, string? IfscCode, DateOnly DateOfJoining, string EmploymentStatus,
        bool PfOptedIn, string PfStatus, DateOnly? PfActiveFrom, Guid SchoolDivisionId);
    public record CreateEmployeeRequest(Guid SchoolDivisionId, string StaffName, StaffType StaffType,
        DateOnly DateOfJoining, string? ContactNumber, string? Email, string? BankAccountNumber, string? IfscCode, bool PfOptedIn);
    public record UpdateEmployeeRequest(string? StaffName, string? ContactNumber, string? Email,
        string? BankAccountNumber, string? IfscCode, EmploymentStatus? EmploymentStatus, bool? PfOptedIn);

    public record PaginatedResult<T>(IReadOnlyList<T> Data, int TotalCount, int Page, int PageSize, int TotalPages);

    /// <summary>GET /employees — Paginated list with filters (doc §6).</summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<EmployeeListDto>>> List(
        [FromQuery] Guid? divisionId, [FromQuery] StaffType? staffType,
        [FromQuery] EmploymentStatus? status, [FromQuery] PfStatus? pfStatus,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _db.Employees.AsQueryable();
        if (divisionId.HasValue) query = query.Where(e => e.SchoolDivisionId == divisionId.Value);
        if (staffType.HasValue) query = query.Where(e => e.StaffType == staffType.Value);
        if (status.HasValue) query = query.Where(e => e.EmploymentStatus == status.Value);
        if (pfStatus.HasValue) query = query.Where(e => e.PfStatus == pfStatus.Value);

        var total = await query.CountAsync();
        var data = await query.OrderBy(e => e.StaffName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new EmployeeListDto(e.Id, e.StaffName, e.StaffType.ToString(), e.EmploymentStatus.ToString(), e.PfStatus.ToString(), e.SchoolDivisionId))
            .ToListAsync();

        return Ok(new PaginatedResult<EmployeeListDto>(data, total, page, pageSize, (int)Math.Ceiling((double)total / pageSize)));
    }

    /// <summary>POST /employees — Create employee (doc §6).</summary>
    [HttpPost]
    public async Task<ActionResult<EmployeeDetailDto>> Create([FromBody] CreateEmployeeRequest request)
    {
        if (_currentUser.SchoolId is null) return BadRequest(new ProblemDetails { Title = "School context required" });

        var employee = new Employee
        {
            SchoolId = _currentUser.SchoolId.Value,
            SchoolDivisionId = request.SchoolDivisionId,
            StaffName = request.StaffName.Trim(),
            StaffType = request.StaffType,
            DateOfJoining = request.DateOfJoining,
            ContactNumber = request.ContactNumber,
            Email = request.Email,
            BankAccountNumber = request.BankAccountNumber,
            IfscCode = request.IfscCode,
            PfOptedIn = request.PfOptedIn
        };
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = employee.Id }, ToDetailDto(employee));
    }

    /// <summary>GET /employees/{id} — Employee detail (doc §6).</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmployeeDetailDto>> Get(Guid id)
    {
        var e = await _db.Employees.FindAsync(id);
        if (e is null) return NotFound();
        return Ok(ToDetailDto(e));
    }

    /// <summary>PUT /employees/{id} — Update employee profile (doc §6).</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EmployeeDetailDto>> Update(Guid id, [FromBody] UpdateEmployeeRequest request)
    {
        var e = await _db.Employees.FindAsync(id);
        if (e is null) return NotFound();

        if (request.StaffName is not null) e.StaffName = request.StaffName.Trim();
        if (request.ContactNumber is not null) e.ContactNumber = request.ContactNumber;
        if (request.Email is not null) e.Email = request.Email;
        if (request.BankAccountNumber is not null) e.BankAccountNumber = request.BankAccountNumber;
        if (request.IfscCode is not null) e.IfscCode = request.IfscCode;
        if (request.EmploymentStatus.HasValue) e.EmploymentStatus = request.EmploymentStatus.Value;
        if (request.PfOptedIn.HasValue) e.PfOptedIn = request.PfOptedIn.Value;

        await _db.SaveChangesAsync();
        return Ok(ToDetailDto(e));
    }

    // ── Salary Components ──

    public record SalaryComponentDto(Guid Id, string ComponentName, string ComponentType, decimal Amount, DateOnly EffectiveFrom, DateOnly? EffectiveTo);
    public record CreateSalaryComponentRequest(ComponentName ComponentName, ComponentType ComponentType, decimal Amount, DateOnly EffectiveFrom);

    /// <summary>GET /employees/{id}/salary-components — List salary components (doc §6).</summary>
    [HttpGet("{id:guid}/salary-components")]
    public async Task<ActionResult<List<SalaryComponentDto>>> ListComponents(Guid id)
    {
        var components = await _db.EmployeeSalaryComponents
            .Where(c => c.EmployeeId == id)
            .OrderByDescending(c => c.EffectiveFrom)
            .Select(c => new SalaryComponentDto(c.Id, c.ComponentName.ToString(), c.ComponentType.ToString(), c.Amount, c.EffectiveFrom, c.EffectiveTo))
            .ToListAsync();
        return Ok(components);
    }

    /// <summary>POST /employees/{id}/salary-components — Add new component or revision (doc §6).
    /// Auto-closes the prior open row of the same component_name.</summary>
    [HttpPost("{id:guid}/salary-components")]
    public async Task<ActionResult<SalaryComponentDto>> AddComponent(Guid id, [FromBody] CreateSalaryComponentRequest request)
    {
        var employee = await _db.Employees.FindAsync(id);
        if (employee is null) return NotFound();

        // Auto-close prior open row of the same component_name
        var prior = await _db.EmployeeSalaryComponents
            .Where(c => c.EmployeeId == id && c.ComponentName == request.ComponentName && c.EffectiveTo == null)
            .FirstOrDefaultAsync();

        if (prior is not null)
        {
            prior.EffectiveTo = request.EffectiveFrom.AddDays(-1);
        }

        var component = new EmployeeSalaryComponent
        {
            EmployeeId = id,
            ComponentName = request.ComponentName,
            ComponentType = request.ComponentType,
            Amount = request.Amount,
            EffectiveFrom = request.EffectiveFrom
        };
        _db.EmployeeSalaryComponents.Add(component);
        await _db.SaveChangesAsync();

        return CreatedAtAction(null, null,
            new SalaryComponentDto(component.Id, component.ComponentName.ToString(), component.ComponentType.ToString(),
                component.Amount, component.EffectiveFrom, component.EffectiveTo));
    }

    /// <summary>PUT /employees/{id}/salary-components/{cid} — Close a component (doc §6).</summary>
    [HttpPut("{id:guid}/salary-components/{cid:guid}")]
    public async Task<IActionResult> CloseComponent(Guid id, Guid cid, [FromBody] DateOnly effectiveTo)
    {
        var component = await _db.EmployeeSalaryComponents.FirstOrDefaultAsync(c => c.Id == cid && c.EmployeeId == id);
        if (component is null) return NotFound();

        component.EffectiveTo = effectiveTo;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── PF Config ──

    public record PfConfigDto(Guid Id, decimal PfGross, DateOnly EffectiveFrom, DateOnly? EffectiveTo);
    public record UpdatePfConfigRequest(decimal PfGross, DateOnly EffectiveFrom, bool? ConfirmEligibility);

    /// <summary>GET /employees/{id}/pf-config — Current PF Gross configuration (doc §6).</summary>
    [HttpGet("{id:guid}/pf-config")]
    public async Task<ActionResult<PfConfigDto?>> GetPfConfig(Guid id)
    {
        var config = await _db.EmployeePfConfigs
            .Where(c => c.EmployeeId == id && c.EffectiveTo == null)
            .OrderByDescending(c => c.EffectiveFrom)
            .FirstOrDefaultAsync();

        if (config is null) return Ok((PfConfigDto?)null);
        return Ok(new PfConfigDto(config.Id, config.PfGross, config.EffectiveFrom, config.EffectiveTo));
    }

    /// <summary>PUT /employees/{id}/pf-config — Update PF Gross / confirm PF eligibility (doc §6).
    /// This doubles as the HR PF-eligibility confirmation action.</summary>
    [HttpPut("{id:guid}/pf-config")]
    public async Task<ActionResult<PfConfigDto>> UpdatePfConfig(Guid id, [FromBody] UpdatePfConfigRequest request)
    {
        var employee = await _db.Employees.FindAsync(id);
        if (employee is null) return NotFound();

        // Close prior
        var prior = await _db.EmployeePfConfigs
            .Where(c => c.EmployeeId == id && c.EffectiveTo == null)
            .FirstOrDefaultAsync();
        if (prior is not null)
            prior.EffectiveTo = request.EffectiveFrom.AddDays(-1);

        var config = new EmployeePfConfig
        {
            EmployeeId = id,
            PfGross = request.PfGross,
            EffectiveFrom = request.EffectiveFrom
        };
        _db.EmployeePfConfigs.Add(config);

        // HR PF confirmation: eligible_pending_confirmation → active
        if (request.ConfirmEligibility == true && employee.PfStatus == PfStatus.EligiblePendingConfirmation)
        {
            employee.PfStatus = PfStatus.Active;
            employee.PfActiveFrom = request.EffectiveFrom;
        }

        await _db.SaveChangesAsync();
        return Ok(new PfConfigDto(config.Id, config.PfGross, config.EffectiveFrom, config.EffectiveTo));
    }

    private static EmployeeDetailDto ToDetailDto(Employee e) => new(
        e.Id, e.StaffName, e.ContactNumber, e.Email, e.StaffType.ToString(),
        e.BankAccountNumber, e.IfscCode, e.DateOfJoining, e.EmploymentStatus.ToString(),
        e.PfOptedIn, e.PfStatus.ToString(), e.PfActiveFrom, e.SchoolDivisionId);
}
