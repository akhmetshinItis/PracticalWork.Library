using Hangfire;
using Microsoft.Extensions.Options;
using PracticalWork.Library.Options;
using PracticalWork.Library.Web.Jobs.Archive;
using PracticalWork.Library.Web.Jobs.Notifications;
using PracticalWork.Library.Web.Jobs.Reports;

namespace PracticalWork.Library.Web.Jobs.Common;

/// <summary>
/// Регистрирует recurring-задачи Hangfire при запуске приложения.
/// </summary>
public sealed class HangfireRecurringJobsHostedService : IHostedService
{
    private readonly IOptions<JobSettings> _jobSettingsOptions;
    private readonly ILogger<HangfireRecurringJobsHostedService> _logger;

    public HangfireRecurringJobsHostedService(
        IOptions<JobSettings> jobSettingsOptions,
        ILogger<HangfireRecurringJobsHostedService> logger)
    {
        _jobSettingsOptions = jobSettingsOptions;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = _jobSettingsOptions.Value;
        ValidateSettings(settings);

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);

        RecurringJob.AddOrUpdate<ReturnReminderJob>(
            LibraryJobNames.ReturnReminder,
            job => job.ExecuteAsync(CancellationToken.None),
            settings.Jobs[LibraryJobNames.ReturnReminder].CronExpression);

        RecurringJob.AddOrUpdate<WeeklyAdminReportJob>(
            LibraryJobNames.WeeklyAdminReport,
            job => job.ExecuteAsync(CancellationToken.None),
            settings.Jobs[LibraryJobNames.WeeklyAdminReport].CronExpression);

        RecurringJob.AddOrUpdate<ArchiveOldBooksJob>(
            LibraryJobNames.ArchiveOldBooks,
            job => job.ExecuteAsync(CancellationToken.None),
            settings.Jobs[LibraryJobNames.ArchiveOldBooks].CronExpression);

        _logger.LogInformation("Hangfire recurring jobs configured for timezone {TimeZone}", settings.TimeZoneId);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static void ValidateSettings(JobSettings settings)
    {
        foreach (var jobName in LibraryJobNames.All)
        {
            if (!settings.Jobs.TryGetValue(jobName, out var configuration))
            {
                throw new InvalidOperationException($"Отсутствует конфигурация задачи '{jobName}' в App:Jobs:Jobs");
            }

            if (!CronExpressionValidator.TryValidate(configuration.CronExpression, out var error))
            {
                throw new InvalidOperationException(
                    $"Некорректный cron для '{jobName}': {configuration.CronExpression}. {error}");
            }

            if (configuration.TimeoutMinutes <= 0)
            {
                throw new InvalidOperationException($"TimeoutMinutes для '{jobName}' должен быть > 0");
            }

            if (configuration.MaxRetries < 0)
            {
                throw new InvalidOperationException($"MaxRetries для '{jobName}' не может быть < 0");
            }
        }
    }
}
