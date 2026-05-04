using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Data.PostgreSql;
using PracticalWork.Library.Data.PostgreSql.Entities;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Models.NotificationModels;
using PracticalWork.Library.Options;
using PracticalWork.Library.Web.Jobs.Common;
using PracticalWork.Library.Web.Services.Email;

namespace PracticalWork.Library.Web.Jobs.Reports;

/// <summary>
/// Еженедельный отчет для администрации библиотеки.
/// </summary>
public sealed class WeeklyAdminReportJob : ILibraryJob
{
    private readonly AppDbContext _dbContext;
    private readonly IMinioService _minioService;
    private readonly IEmailService _emailService;
    private readonly EmailTemplateRenderer _templateRenderer;
    private readonly JobSettings _jobSettings;
    private readonly EmailSettings _emailSettings;
    private readonly EmailTemplateSettings _templateSettings;
    private readonly ILogger<WeeklyAdminReportJob> _logger;

    public WeeklyAdminReportJob(
        AppDbContext dbContext,
        IMinioService minioService,
        IEmailService emailService,
        EmailTemplateRenderer templateRenderer,
        IOptions<JobSettings> jobSettingsOptions,
        IOptions<EmailSettings> emailSettingsOptions,
        IOptions<EmailTemplateSettings> templateSettingsOptions,
        ILogger<WeeklyAdminReportJob> logger)
    {
        _dbContext = dbContext;
        _minioService = minioService;
        _emailService = emailService;
        _templateRenderer = templateRenderer;
        _jobSettings = jobSettingsOptions.Value;
        _emailSettings = emailSettingsOptions.Value;
        _templateSettings = templateSettingsOptions.Value;
        _logger = logger;
    }

    public string JobName => LibraryJobNames.WeeklyAdminReport;

    public string Description => LibraryJobDescriptions.GetDescription(LibraryJobNames.WeeklyAdminReport);

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var configuration = _jobSettings.Jobs[JobName];
        await JobExecutionPolicy.ExecuteAsync(JobName, configuration, _logger, ExecuteCoreAsync, cancellationToken);
    }

    private async Task ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        var template = _templateSettings.WeeklyReport;

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(_jobSettings.TimeZoneId);
        var (periodFrom, periodTo) = GetPreviousWeekPeriod(timeZone);

        var statistics = await CollectStatisticsAsync(periodFrom, periodTo, timeZone, cancellationToken);

        var reportName = $"report_{periodTo:yyyy-MM-dd}.csv";
        var bucket = template.ReportsBucketName;

        var csvContent = BuildCsv(periodFrom, periodTo, statistics);
        await using (var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent)))
        {
            await _minioService.UploadFileAsync(bucket, reportName, csvStream, "text/csv", cancellationToken);
        }

        var reportUrl = await _minioService.GetFileUrlAsync(bucket, reportName, cancellationToken);

        await UpsertReportMetadataAsync(reportName, bucket, periodFrom, periodTo, statistics, cancellationToken);
        await CleanupOldReportsAsync(bucket, template.ReportRetentionDays, cancellationToken);

        var adminEmails = ResolveAdminEmails(template, _emailSettings);
        if (adminEmails.Count == 0)
        {
            _logger.LogInformation("{JobName}: список администраторов пуст, отправка email пропущена", JobName);
            return;
        }

        var placeholders = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["PeriodFrom"] = periodFrom.ToString("dd.MM.yyyy"),
            ["PeriodTo"] = periodTo.ToString("dd.MM.yyyy"),
            ["NewBooksCount"] = statistics.NewBooksCount.ToString(),
            ["NewReadersCount"] = statistics.NewReadersCount.ToString(),
            ["BorrowedCount"] = statistics.BorrowedCount.ToString(),
            ["ReturnedCount"] = statistics.ReturnedCount.ToString(),
            ["OverdueCount"] = statistics.OverdueCount.ToString(),
            ["ReportUrl"] = reportUrl
        };

        var subject = template.SubjectTemplate
            .Replace("{StartDate}", periodFrom.ToString("dd.MM.yyyy"), StringComparison.Ordinal)
            .Replace("{EndDate}", periodTo.ToString("dd.MM.yyyy"), StringComparison.Ordinal);

        var htmlBody = await _templateRenderer.RenderAsync("weekly-report.html", placeholders, cancellationToken);
        var textBody = await _templateRenderer.RenderAsync("weekly-report.txt", placeholders, cancellationToken);

        var notificationLogs = new List<NotificationLogEntity>();
        foreach (var adminEmail in adminEmails)
        {
            var sendResult = await _emailService.SendAsync(new EmailMessage
            {
                To = adminEmail,
                Subject = subject,
                HtmlBody = htmlBody,
                TextBody = textBody
            }, cancellationToken);

            notificationLogs.Add(new NotificationLogEntity
            {
                NotificationType = NotificationTypes.WeeklyReport,
                ReceiverEmail = adminEmail,
                SentAt = DateTime.UtcNow,
                IsSuccess = sendResult.Success,
                ErrorMessage = sendResult.ErrorMessage
            });
        }

        if (notificationLogs.Count > 0)
        {
            await _dbContext.Set<NotificationLogEntity>().AddRangeAsync(notificationLogs, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "{JobName}: отчет {ReportName} создан и отправлен {Count} администраторам",
            JobName,
            reportName,
            adminEmails.Count);
    }

    private async Task<WeeklyStatistics> CollectStatisticsAsync(
        DateOnly periodFrom,
        DateOnly periodTo,
        TimeZoneInfo timeZone,
        CancellationToken cancellationToken)
    {
        var periodFromUtc = TimeZoneInfo.ConvertTimeToUtc(periodFrom.ToDateTime(TimeOnly.MinValue), timeZone);
        var periodToExclusiveUtc = TimeZoneInfo.ConvertTimeToUtc(periodTo.AddDays(1).ToDateTime(TimeOnly.MinValue), timeZone);

        var newBooksCount = await _dbContext.Set<AbstractBookEntity>()
            .AsNoTracking()
            .CountAsync(book => book.CreatedAt >= periodFromUtc && book.CreatedAt < periodToExclusiveUtc, cancellationToken);

        var newReadersCount = await _dbContext.Set<ReaderEntity>()
            .AsNoTracking()
            .CountAsync(reader => reader.CreatedAt >= periodFromUtc && reader.CreatedAt < periodToExclusiveUtc, cancellationToken);

        var borrowedCount = await _dbContext.Set<BookBorrowEntity>()
            .AsNoTracking()
            .CountAsync(borrow => borrow.BorrowDate >= periodFrom && borrow.BorrowDate <= periodTo, cancellationToken);

        var returnedCount = await _dbContext.Set<BookBorrowEntity>()
            .AsNoTracking()
            .CountAsync(borrow =>
                    borrow.ReturnDate >= periodFrom
                    && borrow.ReturnDate <= periodTo
                    && borrow.Status != BookIssueStatus.Issued,
                cancellationToken);

        var overdueCount = await _dbContext.Set<BookBorrowEntity>()
            .AsNoTracking()
            .CountAsync(borrow =>
                    borrow.Status == BookIssueStatus.Issued
                    && borrow.DueDate < periodTo,
                cancellationToken);

        return new WeeklyStatistics(
            newBooksCount,
            newReadersCount,
            borrowedCount,
            returnedCount,
            overdueCount);
    }

    private async Task UpsertReportMetadataAsync(
        string reportName,
        string bucket,
        DateOnly periodFrom,
        DateOnly periodTo,
        WeeklyStatistics statistics,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Set<WeeklyReportMetadataEntity>()
            .SingleOrDefaultAsync(r => r.ReportName == reportName, cancellationToken);

        if (existing is null)
        {
            await _dbContext.Set<WeeklyReportMetadataEntity>().AddAsync(new WeeklyReportMetadataEntity
            {
                ReportName = reportName,
                BucketName = bucket,
                ObjectName = reportName,
                PeriodFrom = periodFrom,
                PeriodTo = periodTo,
                NewBooksCount = statistics.NewBooksCount,
                NewReadersCount = statistics.NewReadersCount,
                BorrowedCount = statistics.BorrowedCount,
                ReturnedCount = statistics.ReturnedCount,
                OverdueCount = statistics.OverdueCount,
                IsDeleted = false
            }, cancellationToken);
        }
        else
        {
            existing.BucketName = bucket;
            existing.ObjectName = reportName;
            existing.PeriodFrom = periodFrom;
            existing.PeriodTo = periodTo;
            existing.NewBooksCount = statistics.NewBooksCount;
            existing.NewReadersCount = statistics.NewReadersCount;
            existing.BorrowedCount = statistics.BorrowedCount;
            existing.ReturnedCount = statistics.ReturnedCount;
            existing.OverdueCount = statistics.OverdueCount;
            existing.IsDeleted = false;
            existing.DeletedAt = null;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task CleanupOldReportsAsync(string bucket, int retentionDays, CancellationToken cancellationToken)
    {
        var cutoffUtc = DateTime.UtcNow.AddDays(-Math.Max(1, retentionDays));

        var oldReports = await _dbContext.Set<WeeklyReportMetadataEntity>()
            .Where(report => !report.IsDeleted && report.CreatedAt < cutoffUtc)
            .ToListAsync(cancellationToken);

        foreach (var report in oldReports)
        {
            try
            {
                await _minioService.RemoveFileAsync(bucket, report.ObjectName, cancellationToken);
                report.IsDeleted = true;
                report.DeletedAt = DateTime.UtcNow;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception,
                    "{JobName}: не удалось удалить старый отчет {ReportName}",
                    JobName,
                    report.ReportName);
            }
        }

        if (oldReports.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static List<string> ResolveAdminEmails(WeeklyReportTemplate template, EmailSettings emailSettings)
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

    private static string BuildCsv(DateOnly periodFrom, DateOnly periodTo, WeeklyStatistics statistics)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ПериодНачало;ПериодКонец;НовыхКниг;НовыхЧитателей;ВыданоКниг;ВозвращеноКниг;ПросроченныхВыдач");
        sb.AppendLine($"{periodFrom:yyyy-MM-dd};{periodTo:yyyy-MM-dd};{statistics.NewBooksCount};{statistics.NewReadersCount};{statistics.BorrowedCount};{statistics.ReturnedCount};{statistics.OverdueCount}");
        return sb.ToString();
    }

    private static (DateOnly periodFrom, DateOnly periodTo) GetPreviousWeekPeriod(TimeZoneInfo timeZone)
    {
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone).Date;
        var daysSinceMonday = ((int)nowLocal.DayOfWeek + 6) % 7;

        var currentWeekMonday = nowLocal.AddDays(-daysSinceMonday);
        var previousWeekMonday = currentWeekMonday.AddDays(-7);
        var previousWeekSunday = currentWeekMonday.AddDays(-1);

        return (DateOnly.FromDateTime(previousWeekMonday), DateOnly.FromDateTime(previousWeekSunday));
    }

    private sealed record WeeklyStatistics(
        int NewBooksCount,
        int NewReadersCount,
        int BorrowedCount,
        int ReturnedCount,
        int OverdueCount);
}
