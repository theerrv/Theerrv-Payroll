using PayrollSaaS.Domain.Entities.Payroll;

namespace PayrollSaaS.Application.Interfaces;

/// <summary>
/// Segregated document-generation interface (doc §7, ISP).
/// Implementations live in Infrastructure (QuestPDF, ClosedXML).
/// </summary>
public interface IDocumentService
{
    /// <summary>Generate a payslip PDF for one entry. Returns the PDF bytes.</summary>
    byte[] GeneratePayslipPdf(PayrollEntry entry, string employeeName, string divisionName, DateOnly payrollMonth);

    /// <summary>Bank transfer CSV for all entries in a run. Returns CSV content as bytes.</summary>
    byte[] GenerateBankCsv(IReadOnlyList<BankTransferRow> rows);

    /// <summary>PF report for a finalized run. Returns CSV bytes.</summary>
    byte[] GeneratePfReport(IReadOnlyList<PfReportRow> rows, DateOnly payrollMonth);

    /// <summary>ESI report for a finalized run. Returns CSV bytes.</summary>
    byte[] GenerateEsiReport(IReadOnlyList<EsiReportRow> rows, DateOnly payrollMonth);

    /// <summary>Full Excel export of a payroll run. Returns .xlsx bytes.</summary>
    byte[] GenerateExcelExport(IReadOnlyList<PayrollExcelRow> rows, string divisionName, DateOnly payrollMonth);
}

// Transfer records for document generation
public record BankTransferRow(string StaffName, string AccountNumber, string IfscCode, decimal NettSalary);
public record PfReportRow(string StaffName, decimal PfGross, decimal EmployerContribution);
public record EsiReportRow(string StaffName, decimal PfGross, decimal EsiAmount);
public record PayrollExcelRow(
    string StaffName, string StaffType, decimal SalaryAfterPf, decimal PfGross, bool IsPfEligible,
    int LopDays, decimal LopDeduction, decimal GrossSalary,
    decimal TotalDeductions, decimal TotalAdditions,
    decimal NettPay, decimal EsiAmount, decimal NettSalary,
    decimal? HrEnteredAmount, bool? AmountMatches);
