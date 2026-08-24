using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayrollSaaS.Domain.Entities.Advances;
using PayrollSaaS.Domain.Enums;

namespace PayrollSaaS.Infrastructure.Persistence.Configurations;

public class AdvanceConfiguration : IEntityTypeConfiguration<Advance>
{
    public void Configure(EntityTypeBuilder<Advance> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Reason).HasMaxLength(500);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasMany(a => a.Installments).WithOne(i => i.Advance).HasForeignKey(i => i.AdvanceId);
    }
}

public class AdvanceInstallmentConfiguration : IEntityTypeConfiguration<AdvanceInstallment>
{
    public void Configure(EntityTypeBuilder<AdvanceInstallment> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(i => new { i.AdvanceId, i.DueMonth }).IsUnique();
    }
}
