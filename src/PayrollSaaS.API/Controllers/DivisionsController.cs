using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollSaaS.Domain.Entities.Tenancy;
using PayrollSaaS.Infrastructure.Persistence;
using PayrollSaaS.Application.Interfaces;

namespace PayrollSaaS.API.Controllers;

[ApiController]
[Route("api/v1/divisions")]
[Authorize]
public class DivisionsController : ControllerBase
{
    private readonly PayrollDbContext _db;
    private readonly ICurrentUserContext _currentUser;

    public DivisionsController(PayrollDbContext db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public record DivisionDto(Guid Id, string Name, bool IsActive, DateTime CreatedAt);
    public record CreateDivisionRequest(string Name);
    public record UpdateDivisionRequest(string? Name, bool? IsActive);

    /// <summary>GET /divisions — List all divisions for this school (doc §6).</summary>
    [HttpGet]
    public async Task<ActionResult<List<DivisionDto>>> List()
    {
        var divisions = await _db.SchoolDivisions
            .OrderBy(d => d.Name)
            .Select(d => new DivisionDto(d.Id, d.Name, d.IsActive, d.CreatedAt))
            .ToListAsync();
        return Ok(divisions);
    }

    /// <summary>POST /divisions — Create new division (school_admin only, doc §6).</summary>
    [HttpPost]
    [Authorize(Policy = "SchoolAdmin")]
    public async Task<ActionResult<DivisionDto>> Create([FromBody] CreateDivisionRequest request)
    {
        if (_currentUser.SchoolId is null)
            return BadRequest(new ProblemDetails { Title = "School context required" });

        var division = new SchoolDivision
        {
            SchoolId = _currentUser.SchoolId.Value,
            Name = request.Name.Trim()
        };
        _db.SchoolDivisions.Add(division);
        await _db.SaveChangesAsync();

        var dto = new DivisionDto(division.Id, division.Name, division.IsActive, division.CreatedAt);
        return CreatedAtAction(null, new { id = division.Id }, dto);
    }

    /// <summary>PUT /divisions/{id} — Update division name or status (doc §6).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "SchoolAdmin")]
    public async Task<ActionResult<DivisionDto>> Update(Guid id, [FromBody] UpdateDivisionRequest request)
    {
        var division = await _db.SchoolDivisions.FindAsync(id);
        if (division is null) return NotFound();

        if (request.Name is not null) division.Name = request.Name.Trim();
        if (request.IsActive is not null) division.IsActive = request.IsActive.Value;

        await _db.SaveChangesAsync();
        return Ok(new DivisionDto(division.Id, division.Name, division.IsActive, division.CreatedAt));
    }
}
