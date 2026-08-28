using ClosedXML.Excel;
using SamanMobileInsurance.Application.Abstractions;
using SamanMobileInsurance.Application.Common;
using SamanMobileInsurance.Application.Reports;
using SamanMobileInsurance.Domain.Enums;

namespace SamanMobileInsurance.Infrastructure.Reports;

public class ExcelReportService : IExcelReportService
{
    private readonly ReportService _reports;

    public ExcelReportService(ReportService reports) => _reports = reports;

    public async Task<byte[]> ExportInsuranceAsync(InsuranceReportFilter filter, CancellationToken cancellationToken = default)
    {
        var rows = await _reports.InsuranceAllAsync(filter, cancellationToken);
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("بیمه‌نامه‌ها");

        var headers = new[]
        {
            "شماره بیمه‌نامه", "نام فروشگاه", "نام مدیر", "موبایل فروشگاه", "استان", "شهر", "آدرس فروشگاه",
            "نام بیمه‌گذار", "نام خانوادگی", "کد ملی", "تاریخ تولد", "موبایل مشتری", "آدرس مشتری", "کد پستی",
            "نوع موبایل", "برند", "مدل", "قیمت (ریال)", "IMEI1", "IMEI2",
            "تاریخ صدور", "تاریخ شروع", "حق بیمه (ریال)", "وضعیت", "وضعیت پرداخت", "شماره تراکنش", "کد پیگیری"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }

        var header = ws.Range(1, 1, 1, headers.Length);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F2744");
        header.Style.Font.FontColor = XLColor.White;

        var r = 2;
        foreach (var row in rows)
        {
            ws.Cell(r, 1).Value = row.PolicyNumber;
            ws.Cell(r, 2).Value = row.StoreName;
            ws.Cell(r, 3).Value = row.ManagerName;
            ws.Cell(r, 4).Value = row.StoreMobile;
            ws.Cell(r, 5).Value = row.Province;
            ws.Cell(r, 6).Value = row.City;
            ws.Cell(r, 7).Value = row.StoreAddress;
            ws.Cell(r, 8).Value = row.CustomerFirstName;
            ws.Cell(r, 9).Value = row.CustomerLastName;
            ws.Cell(r, 10).Value = row.NationalCode;
            ws.Cell(r, 11).Value = IranDateTime.ToJalaliDate(row.BirthDate);
            ws.Cell(r, 12).Value = row.CustomerMobile;
            ws.Cell(r, 13).Value = row.CustomerAddress;
            ws.Cell(r, 14).Value = row.PostalCode;
            ws.Cell(r, 15).Value = PersianLabels.ForInsuranceType(row.InsuranceType);
            ws.Cell(r, 16).Value = row.Brand;
            ws.Cell(r, 17).Value = row.Model;
            ws.Cell(r, 18).Value = row.MobilePriceRial;
            ws.Cell(r, 19).Value = row.Imei1;
            ws.Cell(r, 20).Value = row.Imei2;
            ws.Cell(r, 21).Value = row.IssueDate is null ? "" : IranDateTime.ToJalaliDate(row.IssueDate.Value);
            ws.Cell(r, 22).Value = IranDateTime.ToJalaliDate(row.StartDate);
            ws.Cell(r, 23).Value = row.PremiumRial;
            ws.Cell(r, 24).Value = PersianLabels.ForPolicyStatus(row.Status);
            ws.Cell(r, 25).Value = PersianLabels.ForPaymentStatus(row.PaymentStatus);
            ws.Cell(r, 26).Value = row.TransactionId;
            ws.Cell(r, 27).Value = row.TrackingCode;
            r++;
        }

        ws.RightToLeft = true;
        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
