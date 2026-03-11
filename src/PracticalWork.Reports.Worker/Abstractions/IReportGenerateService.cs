using PracticalWork.Library.Models.ReportModels;

namespace PracticalWork.Reports.Worker.Abstractions;

public interface IReportGenerateService
{
    ReportGenerateResult GenerateReport(Guid reportId, IReadOnlyList<ActivityLog> logs);
}
