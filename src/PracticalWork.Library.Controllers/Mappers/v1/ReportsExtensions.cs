using PracticalWork.Library.Contracts.v1.Reports.Response;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Models.ReportModels;

namespace PracticalWork.Library.Controllers.Mappers.v1;

public static class ReportsExtensions
{
    public static ReportResponse ToReportResponse(this Report report)
    {
        return new ReportResponse(
            report.Name, 
            report.FilePath, 
            report.PeriodFrom, 
            report.PeriodTo, 
            report.EventTypes, 
            report.GeneratedAt);
    }

    public static ReportCreateResponse ToReportCreateResponse(this Report report) =>
        new()
        {
            EventTypes = report.EventTypes,
            PeriodFrom = report.PeriodFrom,
            PeriodTo = report.PeriodTo,
            Status = report.Status,
        };
}

public class ReportCreateResponse
{
    public DateOnly? PeriodFrom { get; set; }
    public DateOnly? PeriodTo { get; set;  }
    public IReadOnlyList<string> EventTypes { get; set; }
    public ReportStatus Status { get; set; }
}