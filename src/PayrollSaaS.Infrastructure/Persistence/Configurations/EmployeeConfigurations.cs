using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayrollSaaS.Domain.Entities.Employees;
using PayrollSaaS.Domain.Enums;

namespace PayrollSaaS.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.StaffName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.ContactNumber).HasMaxLength(20);
        builder.Property(e => e.Email).HasMaxLength(200);
        builder.Property(e => e.BankAccountNumber).HasMaxLength(30);
        builder.Property(e => e.IfscCode).HasMaxLength(15);

        builder.Property(e => e.StaffType).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.EmploymentStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.PfStatus).HasConversion<string>().HasMaxLength(40);

        builder.HasMany(e => e.SalaryComponents).WithOne(c => c.Employee).HasForeignKey(c => c.EmployeeId);
        builder.HasMany(e => e.PfConfigs).WithOne(c => c.Employee).HasForeignKey(c => c.EmployeeId);
    }
}

public class EmployeeSalaryComponentConfiguration : IEntityTypeConfiguration<EmployeeSalaryComponent>
{
    public void Configure(EntityTypeBuilder<EmployeeSalaryComponent> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ComponentName).HasConversion<string>().HasMaxLength(30);
        builder.Property(c => c.ComponentType).HasConversion<string>().HasMaxLength(20);
    }
}

public class EmployeePfConfigConfiguration : IEntityTypeConfiguration<EmployeePfConfig>
{
    public void Configure(EntityTypeBuilder<EmployeePfConfig> builder)
    {
        builder.HasKey(c => c.Id);
    }
}
