using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayrollSaaS.Domain.Entities.Payroll;
using PayrollSaaS.Domain.Enums;

namespace PayrollSaaS.Infrastructure.Persistence.Configurations;

public class PayrollRunConfiguration : IEntityTypeConfiguration<PayrollRun>
{
    public void Configure(EntityTypeBuilder<PayrollRun> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        // One run per division per month (doc §5.5)
        builder.HasIndex(r => new { r.SchoolDivisionId, r.PayrollMonth }).IsUnique();
        builder.HasMany(r => r.Entries).WithOne(e => e.PayrollRun).HasForeignKey(e => e.PayrollRunId);
    }
}

public class PayrollEntryConfiguration : IEntityTypeConfiguration<PayrollEntry>
{
    public void Configure(EntityTypeBuilder<PayrollEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.PayrollRunId, e.EmployeeId }).IsUnique();
        builder.HasMany(e => e.Deductions).WithOne(d => d.PayrollEntry).HasForeignKey(d => d.PayrollEntryId);
        builder.HasMany(e => e.Additions).WithOne(a => a.PayrollEntry).HasForeignKey(a => a.PayrollEntryId);
    }
}

public class PayrollDeductionConfiguration : IEntityTypeConfiguration<PayrollDeduction>
{
    public void Configure(EntityTypeBuilder<PayrollDeduction> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.DeductionType).HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.Description).HasMaxLength(200);
    }
}

public class PayrollAdditionConfiguration : IEntityTypeConfiguration<PayrollAddition>
{
    public void Configure(EntityTypeBuilder<PayrollAddition> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.AdditionType).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Description).HasMaxLength(200);
    }
}
