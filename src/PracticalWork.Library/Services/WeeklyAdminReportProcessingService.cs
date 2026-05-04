using System.Text;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Models.NotificationModels;
using PracticalWork.Library.Models.ReportModels;

namespace PracticalWork.Library.Services;

/// <summary>
/// Сервис сценария подготовки и рассылки еженедельного административного отчета.
/// </summary>
public sealed class WeeklyAdminReportProcessingService : IWeeklyAdminReportProcessingService
{
    private readonly IWeeklyAdminReportRepository _weeklyAdminReportRepository;
    private readonly IWeeklyAdminReportCsvBuilder _weeklyAdminReportCsvBuilder;
    private readonly IWeeklyAdminReportEmailBuilder _weeklyAdminReportEmailBuilder;
    private readonly IMinioService _minioService;
    private readonly IEmailService _emailService;
    private readonly TimeProvider _timeProvider;

    public WeeklyAdminReportProcessingService(
        IWeeklyAdminReportRepository weeklyAdminReportRepository,
        IWeeklyAdminReportCsvBuilder weeklyAdminReportCsvBuilder,
        IWeeklyAdminReportEmailBuilder weeklyAdminReportEmailBuilder,
        IMinioService minioService,
        IEmailService emailService,
        TimeProvider timeProvider)
    {
        _weeklyAdminReportRepository = weeklyAdminReportRepository;
        _weeklyAdminReportCsvBuilder = weeklyAdminReportCsvBuilder;
        _weeklyAdminReportEmailBuilder = weeklyAdminReportEmailBuilder;
        _minioService = minioService;
        _emailService = emailService;
        _timeProvider = timeProvider;
    }

    public async Task<WeeklyAdminReportProcessingResult> ProcessAsync(
        WeeklyAdminReportProcessingRequest request,
        CancellationToken cancellationToken = default)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId);
        var (periodFrom, periodTo) = GetPreviousWeekPeriod(timeZone);
        var statistics = await _weeklyAdminReportRepository.GetStatistics(
            periodFrom,
            periodTo,
            timeZone,
            cancellationToken);

        var reportName = $"report_{periodTo:yyyy-MM-dd}.csv";
        var bucket = request.Template.ReportsBucketName;
        var csvContent = _weeklyAdminReportCsvBuilder.Build(periodFrom, periodTo, statistics);

        await using (var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent)))
        {
            await _minioService.UploadFileAsync(bucket, reportName, csvStream, "text/csv", cancellationToken);
        }

        var reportUrl = await _minioService.GetFileUrlAsync(bucket, reportName, cancellationToken);

        await _weeklyAdminReportRepository.UpsertMetadata(
            new WeeklyAdminReportMetadata
            {
                ReportName = reportName,
                BucketName = bucket,
                ObjectName = reportName,
                PeriodFrom = periodFrom,
                PeriodTo = periodTo,
                Statistics = statistics
            },
            cancellationToken);

        await CleanupOldReportsAsync(bucket, request.Template.ReportRetentionDays, cancellationToken);

        var adminEmails = ResolveAdminEmails(request.Template, request.EmailSettings);
        if (adminEmails.Count == 0)
        {
            return new WeeklyAdminReportProcessingResult
            {
                ReportName = reportName,
                AdminEmailCount = 0
            };
        }

        var successCount = 0;
        var failureCount = 0;
        var logs = new List<WeeklyAdminReportNotificationLogEntry>();

        foreach (var adminEmail in adminEmails)
        {
            var message = await _weeklyAdminReportEmailBuilder.BuildAsync(
                adminEmail,
                periodFrom,
                periodTo,
                statistics,
                reportUrl,
                request.Template,
                cancellationToken);

            var sendResult = await _emailService.SendAsync(message, cancellationToken);
            if (sendResult.Success)
            {
                successCount++;
            }
            else
            {
                failureCount++;
            }

            logs.Add(new WeeklyAdminReportNotificationLogEntry
            {
                ReceiverEmail = adminEmail,
                SentAt = _timeProvider.GetUtcNow().UtcDateTime,
                IsSuccess = sendResult.Success,
                ErrorMessage = sendResult.ErrorMessage
            });
        }

        if (logs.Count > 0)
        {
            await _weeklyAdminReportRepository.SaveNotificationLogs(logs, cancellationToken);
        }

        return new WeeklyAdminReportProcessingResult
        {
            ReportName = reportName,
            AdminEmailCount = adminEmails.Count,
            SuccessCount = successCount,
            FailureCount = failureCount
        };
    }

    private async Task CleanupOldReportsAsync(
        string bucket,
        int retentionDays,
        CancellationToken cancellationToken)
    {
        var cutoffUtc = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-Math.Max(1, retentionDays));
        var oldReports = await _weeklyAdminReportRepository.GetReportsForCleanup(cutoffUtc, cancellationToken);
        if (oldReports.Count == 0)
        {
            return;
        }

        var deletedIds = new List<Guid>();
        foreach (var report in oldReports)
        {
            try
            {
                await _minioService.RemoveFileAsync(bucket, report.ObjectName, cancellationToken);
                deletedIds.Add(report.Id);
            }
            catch
            {
                // Ошибки удаления логирует job-слой через результат обработки не требуется.
            }
        }

        if (deletedIds.Count > 0)
        {
            await _weeklyAdminReportRepository.MarkReportsDeleted(
                deletedIds,
                _timeProvider.GetUtcNow().UtcDateTime,
                cancellationToken);
        }
    }

    private List<string> ResolveAdminEmails(Options.WeeklyReportTemplate template, Options.EmailSettings emailSettings)
    {
        var fromTemplate = template.AdminEmails
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email.Trim())
            .ToList();

        if (fromTemplate.Count > 0)
        {
            return fromTemplate;
        }

        return emailSettings.AdminEmails
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email.Trim())
            .ToList();
    }

    private (DateOnly periodFrom, DateOnly periodTo) GetPreviousWeekPeriod(TimeZoneInfo timeZone)
    {
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(_timeProvider.GetUtcNow().UtcDateTime, timeZone).Date;
        var daysSinceMonday = ((int)nowLocal.DayOfWeek + 6) % 7;

        var currentWeekMonday = nowLocal.AddDays(-daysSinceMonday);
        var previousWeekMonday = currentWeekMonday.AddDays(-7);
        var previousWeekSunday = currentWeekMonday.AddDays(-1);

        return (DateOnly.FromDateTime(previousWeekMonday), DateOnly.FromDateTime(previousWeekSunday));
    }
}
