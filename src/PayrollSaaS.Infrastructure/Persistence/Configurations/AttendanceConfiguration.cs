using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayrollSaaS.Domain.Entities.Attendance;

namespace PayrollSaaS.Infrastructure.Persistence.Configurations;

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.HasKey(a => a.Id);
        // One record per employee per day (doc §5.3)
        builder.HasIndex(a => new { a.EmployeeId, a.AttendanceDate }).IsUnique();
    }
}
