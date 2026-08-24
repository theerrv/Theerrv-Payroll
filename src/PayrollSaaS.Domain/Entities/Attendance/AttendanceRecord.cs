using PayrollSaaS.Domain.Common;

namespace PayrollSaaS.Domain.Entities.Attendance;

/// <summary>
/// One record per employee per calendar day (doc §5.3). Unique (employee_id, attendance_date).
/// LOP days = COUNT(records where is_present = false) for that employee in that month.
/// </summary>
public class AttendanceRecord : BaseEntity
{
    public Guid EmployeeId { get; set; }

    /// <summary>Calendar day — date type, not datetime.</summary>
    public DateOnly AttendanceDate { get; set; }

    /// <summary>false = absent → contributes to LOP count.</summary>
    public bool IsPresent { get; set; }

    /// <summary>FK to users — audit trail for who entered the record.</summary>
    public Guid EnteredBy { get; set; }
}
