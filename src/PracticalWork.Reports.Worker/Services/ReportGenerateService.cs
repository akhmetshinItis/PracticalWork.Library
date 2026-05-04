using System.Text;
using System.Text.Json;
using PracticalWork.Library.Models.ReportModels;
using PracticalWork.Reports.Worker.Abstractions;

namespace PracticalWork.Reports.Worker.Services;

public sealed class ReportGenerateService : IReportGenerateService
{
    private readonly TimeProvider _timeProvider;

    public ReportGenerateService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public ReportGenerateResult GenerateReport(Guid reportId, IReadOnlyList<ActivityLog> logs)
    {
        var timestamp = _timeProvider.GetUtcNow().UtcDateTime;
        var fileName = $"{timestamp.Year}/{timestamp.Month}/{reportId}.csv";
        const string contentType = "text/csv";

        var sb = new StringBuilder();
        sb.AppendLine("EventType;EventDate;Metadata");

        foreach (var log in logs)
        {
            sb.AppendLine($"{log.EventType};{log.EventDate};{JsonSerializer.Serialize(log, log.GetType())}");
        }

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));

        return new ReportGenerateResult
        {
            FileName = fileName,
            Content = stream,
            ContentType = contentType
        };
    }
}
