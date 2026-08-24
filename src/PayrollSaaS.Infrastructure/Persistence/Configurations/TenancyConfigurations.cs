using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayrollSaaS.Domain.Entities.Tenancy;

namespace PayrollSaaS.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Plan).HasMaxLength(50).HasDefaultValue("free");
        builder.HasMany(t => t.Schools).WithOne(s => s.Tenant).HasForeignKey(s => s.TenantId);
    }
}

public class SchoolConfiguration : IEntityTypeConfiguration<School>
{
    public void Configure(EntityTypeBuilder<School> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasMaxLength(300).IsRequired();
        builder.Property(s => s.ContactEmail).HasMaxLength(200);
        builder.Property(s => s.ContactPhone).HasMaxLength(20);
        builder.Property(s => s.Address).HasMaxLength(500);
        builder.HasMany(s => s.Divisions).WithOne(d => d.School).HasForeignKey(d => d.SchoolId);
        builder.HasOne(s => s.PayrollSettings).WithOne(ps => ps.School).HasForeignKey<SchoolPayrollSettings>(ps => ps.SchoolId);
    }
}

public class SchoolDivisionConfiguration : IEntityTypeConfiguration<SchoolDivision>
{
    public void Configure(EntityTypeBuilder<SchoolDivision> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(d => new { d.SchoolId, d.Name }).IsUnique();
    }
}

public class SchoolPayrollSettingsConfiguration : IEntityTypeConfiguration<SchoolPayrollSettings>
{
    public void Configure(EntityTypeBuilder<SchoolPayrollSettings> builder)
    {
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.SchoolId).IsUnique();
    }
}
