using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using PayrollSaaS.Application.Interfaces;
using PayrollSaaS.Domain.Entities.Payroll;
using PayrollSaaS.Shared.Money;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PayrollSaaS.Infrastructure.Documents;

public sealed class DocumentService : IDocumentService
{
    public byte[] GeneratePayslipPdf(PayrollEntry entry, string employeeName, string divisionName, DateOnly payrollMonth)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);

                page.Header().Column(col =>
                {
                    col.Item().Text("PAYSLIP").FontSize(18).Bold().AlignCenter();
                    col.Item().Text($"{divisionName} — {payrollMonth:MMMM yyyy}").FontSize(12).AlignCenter();
                    col.Item().PaddingBottom(10).LineHorizontal(1);
                });

                page.Content().Column(col =>
                {
                    col.Spacing(5);

                    // Employee info
                    col.Item().Text($"Employee: {employeeName}").FontSize(11).Bold();
                    col.Item().PaddingBottom(10);

                    // Earnings
                    col.Item().Text("Earnings").FontSize(11).Bold().Underline();
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Salary After PF");
                        row.ConstantItem(100).AlignRight().Text(MoneyMath.ToApiString(entry.SalaryAfterPf));
                    });
                    if (entry.IsPfEligible)
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("PF Gross");
                            row.ConstantItem(100).AlignRight().Text(MoneyMath.ToApiString(entry.PfGross));
                        });
                    }
                    col.Item().PaddingTop(5);

                    // Deductions
                    col.Item().Text("Deductions").FontSize(11).Bold().Underline();
                    foreach (var d in entry.Deductions)
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text(d.Description);
                            row.ConstantItem(100).AlignRight().Text(MoneyMath.ToApiString(d.Amount));
                        });
                    }
                    if (entry.EsiAmount > 0)
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("ESI (Employee)");
                            row.ConstantItem(100).AlignRight().Text(MoneyMath.ToApiString(entry.EsiAmount));
                        });
                    }
                    col.Item().PaddingTop(5);

                    // Additions
                    if (entry.Additions.Count > 0)
                    {
                        col.Item().Text("Additions").FontSize(11).Bold().Underline();
                        foreach (var a in entry.Additions)
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text(a.Description);
                                row.ConstantItem(100).AlignRight().Text(MoneyMath.ToApiString(a.Amount));
                            });
                        }
                        col.Item().PaddingTop(5);
                    }

                    // Summary
                    col.Item().LineHorizontal(1);
                    col.Item().PaddingTop(5);
                    SummaryRow(col, "Gross Salary", entry.GrossSalary);
                    SummaryRow(col, "LOP Days", entry.LopDays);
                    SummaryRow(col, "Total Deductions", entry.TotalDeductions);
                    SummaryRow(col, "Total Additions", entry.TotalAdditions);
                    SummaryRow(col, "NETT Pay", entry.NettPay);
                    SummaryRow(col, "ESI", entry.EsiAmount);
                    col.Item().LineHorizontal(1);
                    col.Item().PaddingTop(3).Row(row =>
                    {
                        row.RelativeItem().Text("NETT Salary").FontSize(13).Bold();
                        row.ConstantItem(100).AlignRight().Text(MoneyMath.RoundPayable(entry.NettSalary).ToString("F2")).FontSize(13).Bold();
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Generated on ");
                    t.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm UTC"));
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void SummaryRow(ColumnDescriptor col, string label, decimal value)
    {
        col.Item().Row(row =>
        {
            row.RelativeItem().Text(label);
            row.ConstantItem(100).AlignRight().Text(MoneyMath.ToApiString(value));
        });
    }

    private static void SummaryRow(ColumnDescriptor col, string label, int value)
    {
        col.Item().Row(row =>
        {
            row.RelativeItem().Text(label);
            row.ConstantItem(100).AlignRight().Text(value.ToString());
        });
    }

    public byte[] GenerateBankCsv(IReadOnlyList<BankTransferRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Staff Name,Account Number,IFSC Code,NETT Salary");
        foreach (var r in rows)
            sb.AppendLine($"{Csv(r.StaffName)},{Csv(r.AccountNumber)},{Csv(r.IfscCode)},{MoneyMath.RoundPayable(r.NettSalary):F2}");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public byte[] GeneratePfReport(IReadOnlyList<PfReportRow> rows, DateOnly payrollMonth)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"PF Report — {payrollMonth:MMMM yyyy}");
        sb.AppendLine("Staff Name,PF Gross,Employer Contribution");
        foreach (var r in rows)
            sb.AppendLine($"{Csv(r.StaffName)},{MoneyMath.ToApiString(r.PfGross)},{MoneyMath.ToApiString(r.EmployerContribution)}");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public byte[] GenerateEsiReport(IReadOnlyList<EsiReportRow> rows, DateOnly payrollMonth)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"ESI Report — {payrollMonth:MMMM yyyy}");
        sb.AppendLine("Staff Name,PF Gross,ESI Amount");
        foreach (var r in rows)
            sb.AppendLine($"{Csv(r.StaffName)},{MoneyMath.ToApiString(r.PfGross)},{MoneyMath.ToApiString(r.EsiAmount)}");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public byte[] GenerateExcelExport(IReadOnlyList<PayrollExcelRow> rows, string divisionName, DateOnly payrollMonth)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add($"Payroll {payrollMonth:yyyy-MM}");

        // Headers
        var headers = new[]
        {
            "Staff Name", "Staff Type", "Salary After PF", "PF Gross", "PF Eligible",
            "LOP Days", "LOP Deduction", "Gross Salary",
            "Total Deductions", "Total Additions",
            "NETT Pay", "ESI Amount", "NETT Salary",
            "HR Entered Amount", "Amount Matches"
        };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
        }

        // Data
        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            var xlRow = r + 2;
            ws.Cell(xlRow, 1).Value = row.StaffName;
            ws.Cell(xlRow, 2).Value = row.StaffType;
            ws.Cell(xlRow, 3).Value = (double)row.SalaryAfterPf;
            ws.Cell(xlRow, 4).Value = (double)row.PfGross;
            ws.Cell(xlRow, 5).Value = row.IsPfEligible ? "Yes" : "No";
            ws.Cell(xlRow, 6).Value = row.LopDays;
            ws.Cell(xlRow, 7).Value = (double)row.LopDeduction;
            ws.Cell(xlRow, 8).Value = (double)row.GrossSalary;
            ws.Cell(xlRow, 9).Value = (double)row.TotalDeductions;
            ws.Cell(xlRow, 10).Value = (double)row.TotalAdditions;
            ws.Cell(xlRow, 11).Value = (double)row.NettPay;
            ws.Cell(xlRow, 12).Value = (double)row.EsiAmount;
            ws.Cell(xlRow, 13).Value = (double)row.NettSalary;
            ws.Cell(xlRow, 14).Value = row.HrEnteredAmount.HasValue ? (double)row.HrEnteredAmount.Value : "";
            ws.Cell(xlRow, 15).Value = row.AmountMatches?.ToString() ?? "";
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    /// <summary>Escape CSV field if it contains commas, quotes, or newlines.</summary>
    private static string Csv(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
