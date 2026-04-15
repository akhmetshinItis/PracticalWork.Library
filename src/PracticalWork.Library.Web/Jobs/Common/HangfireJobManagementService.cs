using Hangfire;
using Microsoft.Extensions.Options;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Models.JobModels;
using PracticalWork.Library.Options;

namespace PracticalWork.Library.Web.Jobs.Common;

/// <summary>
/// Сервис управления задачами Hangfire.
/// </summary>
public sealed class HangfireJobManagementService : IJobManagementService
{
    private readonly IOptions<JobSettings> _jobSettingsOptions;

    public HangfireJobManagementService(IOptions<JobSettings> jobSettingsOptions)
    {
        _jobSettingsOptions = jobSettingsOptions;
    }

    public IReadOnlyList<ScheduledJobInfo> GetJobs()
    {
        var settings = _jobSettingsOptions.Value;
        return LibraryJobNames.All
            .Where(jobName => settings.Jobs.ContainsKey(jobName))
            .Select(jobName => new ScheduledJobInfo
            {
                Name = jobName,
                Description = LibraryJobDescriptions.GetDescription(jobName),
                CronExpression = settings.Jobs[jobName].CronExpression,
                TimeZoneId = settings.TimeZoneId
            })
            .ToList();
    }

    public Task TriggerAsync(string jobName, CancellationToken cancellationToken = default)
    {
        var actualJobName = LibraryJobNames.All
            .FirstOrDefault(name => string.Equals(name, jobName, StringComparison.OrdinalIgnoreCase));

        if (actualJobName is null)
        {
            throw new ArgumentException($"Неизвестная задача: {jobName}", nameof(jobName));
        }

        RecurringJob.TriggerJob(actualJobName);
        return Task.CompletedTask;
    }
}
