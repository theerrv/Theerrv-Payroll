using Microsoft.EntityFrameworkCore;
using PayrollSaaS.Application.Interfaces;
using PayrollSaaS.Domain.Entities.Advances;
using PayrollSaaS.Domain.Entities.Attendance;
using PayrollSaaS.Domain.Entities.Auth;
using PayrollSaaS.Domain.Entities.Employees;
using PayrollSaaS.Domain.Entities.Payroll;
using PayrollSaaS.Domain.Entities.Tenancy;

namespace PayrollSaaS.Infrastructure.Persistence;

/// <summary>
/// Central EF Core context. All payroll tables live in the "payroll" schema so they are not
/// exposed by Supabase's auto-generated PostgREST API (which only serves "public").
/// </summary>
public class PayrollDbContext : DbContext, IPayrollDbContext
{
    private readonly ICurrentUserContext? _currentUser;

    public PayrollDbContext(DbContextOptions<PayrollDbContext> options, ICurrentUserContext? currentUser = null)
        : base(options)
    {
        _currentUser = currentUser;
    }

    // ── DbSets ──
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<School> Schools => Set<School>();
    public DbSet<SchoolDivision> SchoolDivisions => Set<SchoolDivision>();
    public DbSet<SchoolPayrollSettings> SchoolPayrollSettings => Set<SchoolPayrollSettings>();

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeSalaryComponent> EmployeeSalaryComponents => Set<EmployeeSalaryComponent>();
    public DbSet<EmployeePfConfig> EmployeePfConfigs => Set<EmployeePfConfig>();

    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    public DbSet<Advance> Advances => Set<Advance>();
    public DbSet<AdvanceInstallment> AdvanceInstallments => Set<AdvanceInstallment>();

    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<PayrollEntry> PayrollEntries => Set<PayrollEntry>();
    public DbSet<PayrollDeduction> PayrollDeductions => Set<PayrollDeduction>();
    public DbSet<PayrollAddition> PayrollAdditions => Set<PayrollAddition>();

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // All payroll tables in "payroll" schema — kept out of Supabase's "public" PostgREST.
        modelBuilder.HasDefaultSchema("payroll");

        // Apply all IEntityTypeConfiguration<T> from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PayrollDbContext).Assembly);

        // ── Global query filters for tenant/school isolation ──
        // super_admin bypasses (SchoolId is null in ICurrentUserContext).
        if (_currentUser is not null && !_currentUser.IsSuperAdmin && _currentUser.SchoolId is not null)
        {
            var schoolId = _currentUser.SchoolId.Value;

            modelBuilder.Entity<School>().HasQueryFilter(s => s.Id == schoolId);
            modelBuilder.Entity<SchoolDivision>().HasQueryFilter(d => d.SchoolId == schoolId);
            modelBuilder.Entity<Employee>().HasQueryFilter(e => e.SchoolId == schoolId);
            modelBuilder.Entity<Advance>().HasQueryFilter(a => a.SchoolId == schoolId);
            modelBuilder.Entity<PayrollRun>().HasQueryFilter(r => r.SchoolId == schoolId);
            modelBuilder.Entity<User>().HasQueryFilter(u => u.SchoolId == schoolId || u.SchoolId == null);
        }

        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // All decimal → decimal(18,4) — the storage scale for money (doc §5).
        configurationBuilder.Properties<decimal>().HavePrecision(18, 4);

        base.ConfigureConventions(configurationBuilder);
    }
}
