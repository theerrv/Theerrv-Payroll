using Microsoft.EntityFrameworkCore;
using PayrollSaaS.Domain.Entities.Advances;
using PayrollSaaS.Domain.Entities.Attendance;
using PayrollSaaS.Domain.Entities.Auth;
using PayrollSaaS.Domain.Entities.Employees;
using PayrollSaaS.Domain.Entities.Tenancy;
using PayrollSaaS.Domain.Enums;
using PayrollSaaS.Infrastructure.Persistence;

namespace PayrollSaaS.API.Auth;

/// <summary>
/// Seeds one tenant, one school, two divisions, one user per role, and realistic dummy
/// employees with salary components, attendance and one advance for development.
/// Idempotent — skips if the tenant already exists.
/// </summary>
public static class SeedData
{
    public static async Task SeedAsync(PayrollDbContext db)
    {
        if (await db.Tenants.IgnoreQueryFilters().AnyAsync()) return;

        var tenantId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();
        var matricId = Guid.NewGuid();
        var icseId = Guid.NewGuid();

        var tenant = new Tenant { Id = tenantId, Name = "Theerrv Education Group", Plan = "free", IsActive = true };
        var school = new School
        {
            Id = schoolId, TenantId = tenantId, Name = "Theerrv School",
            ContactEmail = "admin@theerrv.edu", ContactPhone = "+91-9876543210"
        };
        var settings = new SchoolPayrollSettings
        {
            SchoolId = schoolId, EmployerPfRate = 0.12m, EsiRate = 0.0075m
        };
        var matric = new SchoolDivision { Id = matricId, SchoolId = schoolId, Name = "Matric" };
        var icse = new SchoolDivision { Id = icseId, SchoolId = schoolId, Name = "ICSE" };

        db.Tenants.Add(tenant);
        db.Schools.Add(school);
        db.SchoolPayrollSettings.Add(settings);
        db.SchoolDivisions.AddRange(matric, icse);

        var hash = BCrypt.Net.BCrypt.HashPassword("Password123!");
        var users = new[]
        {
            new User { SchoolId = null,     Email = "superadmin@payroll.dev", PasswordHash = hash, Role = UserRole.SuperAdmin },
            new User { SchoolId = schoolId, Email = "admin@theerrv.edu",      PasswordHash = hash, Role = UserRole.SchoolAdmin },
            new User { SchoolId = schoolId, Email = "hr@theerrv.edu",         PasswordHash = hash, Role = UserRole.Hr },
            new User { SchoolId = schoolId, Email = "finance@theerrv.edu",    PasswordHash = hash, Role = UserRole.Finance },
            new User { SchoolId = schoolId, Email = "employee@theerrv.edu",   PasswordHash = hash, Role = UserRole.Employee },
        };
        db.Users.AddRange(users);
        await db.SaveChangesAsync();

        // ── Dummy employees ─────────────────────────────────────────────
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var joinedTwoYearsAgo = today.AddYears(-2);
        var joinedOneYearAgo  = today.AddYears(-1).AddMonths(-3);
        var joinedRecently    = today.AddMonths(-4);

        // (name, division, staffType, joining, pfOptedIn, pfStatus, pfActiveFrom, basic, hra, da, special)
        var empDefs = new[]
        {
            ("Anitha Ramachandran", matricId, StaffType.Teaching,    joinedTwoYearsAgo, true,  PfStatus.Active, (DateOnly?)joinedTwoYearsAgo.AddYears(1),  18000m, 7200m, 1800m, 5000m),
            ("Karthik Subramaniam", matricId, StaffType.Teaching,    joinedTwoYearsAgo, true,  PfStatus.Active, (DateOnly?)joinedTwoYearsAgo.AddYears(1),  22000m, 8800m, 2200m, 5000m),
            ("Meena Sundaram",      matricId, StaffType.NonTeaching, joinedOneYearAgo,  true,  PfStatus.Active, (DateOnly?)joinedOneYearAgo.AddMonths(3),   12000m, 4800m, 1200m, 2000m),
            ("Rajesh Kannan",       matricId, StaffType.Teaching,    joinedRecently,    true,  PfStatus.NotEligible, null,                                  15000m, 6000m, 1500m, 2500m),
            ("Priya Venkatesh",     icseId,   StaffType.Teaching,    joinedTwoYearsAgo, true,  PfStatus.Active, (DateOnly?)joinedTwoYearsAgo.AddYears(1),  20000m, 8000m, 2000m, 5000m),
            ("Deepak Mohan",        icseId,   StaffType.Teaching,    joinedTwoYearsAgo, false, PfStatus.NotEligible, null,                                  17000m, 6800m, 1700m, 4500m),
            ("Lakshmi Narayan",     icseId,   StaffType.NonTeaching, joinedOneYearAgo,  true,  PfStatus.Active, (DateOnly?)joinedOneYearAgo.AddMonths(3),   10000m, 4000m, 1000m, 2000m),
            ("Senthil Kumar",       icseId,   StaffType.NonTeaching, joinedRecently,    false, PfStatus.NotEligible, null,                                   9000m, 3600m,  900m, 1500m),
        };

        var employees = new List<Employee>();
        var effectiveFrom = new DateOnly(today.Year, today.Month, 1).AddMonths(-3);

        foreach (var (name, divId, sType, joining, pfOpted, pfStatus, pfActiveFrom, basic, hra, da, special) in empDefs)
        {
            var emp = new Employee
            {
                SchoolId          = schoolId,
                SchoolDivisionId  = divId,
                StaffName         = name,
                Email             = name.ToLower().Replace(" ", ".") + "@theerrv.edu",
                ContactNumber     = "+91-98765" + (43000 + employees.Count),
                BankAccountNumber = "5000" + (1000 + employees.Count).ToString(),
                IfscCode          = "SBIN0001234",
                StaffType         = sType,
                DateOfJoining     = joining,
                EmploymentStatus  = EmploymentStatus.Active,
                PfOptedIn         = pfOpted,
                PfStatus          = pfStatus,
                PfActiveFrom      = pfActiveFrom,
            };

            emp.SalaryComponents.Add(new EmployeeSalaryComponent { EmployeeId = emp.Id, ComponentName = ComponentName.Basic,           ComponentType = ComponentType.Earning, Amount = basic,   EffectiveFrom = effectiveFrom });
            emp.SalaryComponents.Add(new EmployeeSalaryComponent { EmployeeId = emp.Id, ComponentName = ComponentName.Hra,             ComponentType = ComponentType.Earning, Amount = hra,     EffectiveFrom = effectiveFrom });
            emp.SalaryComponents.Add(new EmployeeSalaryComponent { EmployeeId = emp.Id, ComponentName = ComponentName.Da,              ComponentType = ComponentType.Earning, Amount = da,      EffectiveFrom = effectiveFrom });
            emp.SalaryComponents.Add(new EmployeeSalaryComponent { EmployeeId = emp.Id, ComponentName = ComponentName.SpecialAllowance, ComponentType = ComponentType.Earning, Amount = special, EffectiveFrom = effectiveFrom });

            if (pfStatus == PfStatus.Active)
                emp.PfConfigs.Add(new EmployeePfConfig { EmployeeId = emp.Id, PfGross = basic, EffectiveFrom = effectiveFrom });

            employees.Add(emp);
        }

        db.Employees.AddRange(employees);
        await db.SaveChangesAsync();

        // ── Attendance for current month (all present except a few LOPs) ──
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var attendance = new List<AttendanceRecord>();

        // Employees with LOP: index 1 (2 days absent), index 5 (1 day absent)
        var lopDays = new Dictionary<int, HashSet<int>>
        {
            [1] = [3, 10],   // Karthik absent on 3rd and 10th
            [5] = [7],       // Deepak absent on 7th
        };

        for (var empIdx = 0; empIdx < employees.Count; empIdx++)
        {
            for (var day = 1; day < today.Day; day++)
            {
                var date = new DateOnly(today.Year, today.Month, day);
                var isAbsent = lopDays.TryGetValue(empIdx, out var absent) && absent.Contains(day);
                attendance.Add(new AttendanceRecord
                {
                    EmployeeId     = employees[empIdx].Id,
                    AttendanceDate = date,
                    IsPresent      = !isAbsent,
                    EnteredBy      = users[2].Id  // HR user
                });
            }
        }

        db.AttendanceRecords.AddRange(attendance);

        // ── One advance for Meena (index 2) — 3-month recovery ──
        var meena = employees[2];
        var recoveryStart = monthStart;
        var advance = new Advance
        {
            EmployeeId          = meena.Id,
            SchoolId            = schoolId,
            TotalAmount         = 6000m,
            Reason              = "Medical emergency",
            GivenDate           = today.AddMonths(-1),
            RecoveryStartMonth  = recoveryStart,
            InstallmentAmount   = 2000m,
            TotalInstallments   = 3,
            InstallmentsRecovered = 0,
            BalanceAmount       = 6000m,
            Status              = AdvanceStatus.Active,
        };
        advance.Installments.Add(new AdvanceInstallment { AdvanceId = advance.Id, DueMonth = recoveryStart,            Amount = 2000m, Status = InstallmentStatus.Pending });
        advance.Installments.Add(new AdvanceInstallment { AdvanceId = advance.Id, DueMonth = recoveryStart.AddMonths(1), Amount = 2000m, Status = InstallmentStatus.Pending });
        advance.Installments.Add(new AdvanceInstallment { AdvanceId = advance.Id, DueMonth = recoveryStart.AddMonths(2), Amount = 2000m, Status = InstallmentStatus.Pending });

        db.Advances.Add(advance);
        await db.SaveChangesAsync();
    }
}
