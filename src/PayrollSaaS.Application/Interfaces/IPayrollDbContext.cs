using Microsoft.EntityFrameworkCore;
using PayrollSaaS.Domain.Entities.Advances;
using PayrollSaaS.Domain.Entities.Attendance;
using PayrollSaaS.Domain.Entities.Auth;
using PayrollSaaS.Domain.Entities.Employees;
using PayrollSaaS.Domain.Entities.Payroll;
using PayrollSaaS.Domain.Entities.Tenancy;

namespace PayrollSaaS.Application.Interfaces;

/// <summary>
/// Narrow interface over PayrollDbContext — exposes DbSets + SaveChangesAsync.
/// No repository wrapper; reusable queries are IQueryable extension methods.
/// </summary>
public interface IPayrollDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<School> Schools { get; }
    DbSet<SchoolDivision> SchoolDivisions { get; }
    DbSet<SchoolPayrollSettings> SchoolPayrollSettings { get; }

    DbSet<Employee> Employees { get; }
    DbSet<EmployeeSalaryComponent> EmployeeSalaryComponents { get; }
    DbSet<EmployeePfConfig> EmployeePfConfigs { get; }

    DbSet<AttendanceRecord> AttendanceRecords { get; }

    DbSet<Advance> Advances { get; }
    DbSet<AdvanceInstallment> AdvanceInstallments { get; }

    DbSet<PayrollRun> PayrollRuns { get; }
    DbSet<PayrollEntry> PayrollEntries { get; }
    DbSet<PayrollDeduction> PayrollDeductions { get; }
    DbSet<PayrollAddition> PayrollAdditions { get; }

    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
