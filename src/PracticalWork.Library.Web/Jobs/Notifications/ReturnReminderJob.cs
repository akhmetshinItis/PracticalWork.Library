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

namespace PracticalWork.Library.Web.Jobs.Notifications;

/// <summary>
/// Ежедневная задача отправки напоминаний о возврате книг.
/// </summary>
public sealed class ReturnReminderJob : ILibraryJob
{
    private readonly AppDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly EmailTemplateRenderer _templateRenderer;
    private readonly IOptions<JobSettings> _jobSettingsOptions;
    private readonly IOptions<EmailTemplateSettings> _templateSettingsOptions;
    private readonly ILogger<ReturnReminderJob> _logger;

    public ReturnReminderJob(
        AppDbContext dbContext,
        IEmailService emailService,
        EmailTemplateRenderer templateRenderer,
        IOptions<JobSettings> jobSettingsOptions,
        IOptions<EmailTemplateSettings> templateSettingsOptions,
        ILogger<ReturnReminderJob> logger)
    {
        _dbContext = dbContext;
        _emailService = emailService;
        _templateRenderer = templateRenderer;
        _jobSettingsOptions = jobSettingsOptions;
        _templateSettingsOptions = templateSettingsOptions;
        _logger = logger;
    }

    public string JobName => LibraryJobNames.ReturnReminder;

    public string Description => LibraryJobDescriptions.GetDescription(LibraryJobNames.ReturnReminder);

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var configuration = _jobSettingsOptions.Value.Jobs[JobName];
        await JobExecutionPolicy.ExecuteAsync(JobName, configuration, _logger, ExecuteCoreAsync, cancellationToken);
    }

    private async Task ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        var template = _templateSettingsOptions.Value.ReturnReminder;
        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(template.DaysBeforeDueDate));
        var duplicateCutoff = DateTime.UtcNow.AddHours(-24);

        var candidates = await (
            from borrow in _dbContext.Set<BookBorrowEntity>().AsNoTracking()
            join reader in _dbContext.Set<ReaderEntity>().AsNoTracking() on borrow.ReaderId equals reader.Id
            join book in _dbContext.Set<AbstractBookEntity>().AsNoTracking() on borrow.BookId equals book.Id
            where borrow.Status == BookIssueStatus.Issued
                  && borrow.DueDate == dueDate
                  && reader.IsActive
            select new ReminderCandidate
            {
                BorrowId = borrow.Id,
                ReaderId = reader.Id,
                ReaderFullName = reader.FullName,
                ReaderEmail = reader.Email,
                BookId = book.Id,
                BookTitle = book.Title,
                BookAuthors = book.Authors.ToArray(),
                DueDate = borrow.DueDate
            }).ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            _logger.LogInformation("{JobName}: кандидаты на отправку не найдены", JobName);
            return;
        }

        var borrowIds = candidates.Select(c => c.BorrowId).ToList();
        var alreadyNotifiedBorrowIds = await _dbContext.Set<NotificationLogEntity>()
            .AsNoTracking()
            .Where(log => log.NotificationType == NotificationTypes.ReturnReminder
                          && log.IsSuccess
                          && log.BorrowId.HasValue
                          && borrowIds.Contains(log.BorrowId.Value)
                          && log.SentAt >= duplicateCutoff)
            .Select(log => log.BorrowId!.Value)
            .ToListAsync(cancellationToken);

        var alreadyNotified = alreadyNotifiedBorrowIds.ToHashSet();

        var notificationLogs = new List<NotificationLogEntity>();
        var successCount = 0;
        var failureCount = 0;
        var skippedCount = 0;

        foreach (var candidate in candidates)
        {
            if (alreadyNotified.Contains(candidate.BorrowId))
            {
                skippedCount++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(candidate.ReaderEmail))
            {
                failureCount++;
                notificationLogs.Add(CreateNotificationLog(candidate, false, "У читателя не указан email"));
                continue;
            }

            var placeholders = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["ReaderFullName"] = candidate.ReaderFullName,
                ["BookTitle"] = candidate.BookTitle,
                ["BookAuthors"] = string.Join(", ", candidate.BookAuthors),
                ["DueDate"] = candidate.DueDate.ToString("dd.MM.yyyy"),
                ["DaysLeft"] = template.DaysBeforeDueDate.ToString(),
                ["LibraryAddress"] = template.LibraryAddress,
                ["LibraryPhone"] = template.LibraryPhone,
                ["LibraryHours"] = template.WorkingHours
            };

            var subject = template.SubjectTemplate
                .Replace("{BookTitle}", candidate.BookTitle, StringComparison.Ordinal);

            var htmlBody = await _templateRenderer
                .RenderAsync("return-reminder.html", placeholders, cancellationToken);
            var textBody = await _templateRenderer
                .RenderAsync("return-reminder.txt", placeholders, cancellationToken);

            var sendResult = await _emailService.SendAsync(new EmailMessage
            {
                To = candidate.ReaderEmail,
                Subject = subject,
                HtmlBody = htmlBody,
                TextBody = textBody
            }, cancellationToken);

            if (sendResult.Success)
            {
                successCount++;
            }
            else
            {
                failureCount++;
            }

            notificationLogs.Add(CreateNotificationLog(candidate, sendResult.Success, sendResult.ErrorMessage));
        }

        if (notificationLogs.Count > 0)
        {
            await _dbContext.Set<NotificationLogEntity>().AddRangeAsync(notificationLogs, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "{JobName}: завершено. Success={Success}, Failed={Failed}, Skipped={Skipped}",
            JobName,
            successCount,
            failureCount,
            skippedCount);
    }

    private static NotificationLogEntity CreateNotificationLog(
        ReminderCandidate candidate,
        bool success,
        string errorMessage)
    {
        return new NotificationLogEntity
        {
            NotificationType = NotificationTypes.ReturnReminder,
            ReaderId = candidate.ReaderId,
            BookId = candidate.BookId,
            BorrowId = candidate.BorrowId,
            ReceiverEmail = candidate.ReaderEmail,
            SentAt = DateTime.UtcNow,
            IsSuccess = success,
            ErrorMessage = errorMessage
        };
    }

    private sealed class ReminderCandidate
    {
        public Guid BorrowId { get; init; }
        public Guid ReaderId { get; init; }
        public required string ReaderFullName { get; init; }
        public required string ReaderEmail { get; init; }
        public Guid BookId { get; init; }
        public required string BookTitle { get; init; }
        public required string[] BookAuthors { get; init; }
        public DateOnly DueDate { get; init; }
    }
}
