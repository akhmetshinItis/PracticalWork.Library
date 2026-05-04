using Microsoft.Extensions.Options;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Models.BookModels;
using PracticalWork.Library.Options;
using PracticalWork.Library.Web.Jobs.Common;

namespace PracticalWork.Library.Web.Jobs.Archive;

/// <summary>
/// Ежемесячная архивация старых книг.
/// </summary>
public sealed class ArchiveOldBooksJob : ILibraryJob
{
    private readonly IBookArchiveRepository _bookArchiveRepository;
    private readonly IBookService _bookService;
    private readonly JobSettings _jobSettings;
    private readonly ArchiveSettings _archiveSettings;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ArchiveOldBooksJob> _logger;

    public ArchiveOldBooksJob(
        IBookArchiveRepository bookArchiveRepository,
        IBookService bookService,
        IOptions<JobSettings> jobSettingsOptions,
        IOptions<ArchiveSettings> archiveSettingsOptions,
        TimeProvider timeProvider,
        ILogger<ArchiveOldBooksJob> logger)
    {
        _bookArchiveRepository = bookArchiveRepository;
        _bookService = bookService;
        _jobSettings = jobSettingsOptions.Value;
        _archiveSettings = archiveSettingsOptions.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public string JobName => LibraryJobNames.ArchiveOldBooks;

    public string Description => LibraryJobDescriptions.GetDescription(LibraryJobNames.ArchiveOldBooks);

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var configuration = _jobSettings.Jobs[JobName];
        await JobExecutionPolicy.ExecuteAsync(JobName, configuration, _logger, ExecuteCoreAsync, cancellationToken);
    }

    private async Task ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var thresholdDate = DateOnly.FromDateTime(utcNow.Date.AddYears(-_archiveSettings.YearsWithoutBorrow));
        var maxBooks = Math.Max(1, _archiveSettings.MaxBooksPerRun);

        var candidates = await _bookArchiveRepository.GetArchiveCandidates(
            thresholdDate,
            maxBooks,
            cancellationToken);

        if (candidates.Count == 0)
        {
            _logger.LogInformation("{JobName}: книги для архивации не найдены", JobName);
            return;
        }

        var runId = Guid.NewGuid();
        var startTime = utcNow;
        var archivedCount = 0;
        var skippedCount = 0;
        var failedCount = 0;

        var logs = new List<ArchiveBookLogEntry>();

        foreach (var candidate in candidates)
        {
            if (candidate.BookStatus != BookStatus.Available)
            {
                skippedCount++;
                logs.Add(CreateBookLog(
                    runId,
                    candidate,
                    "skipped",
                    $"Текущий статус книги '{candidate.BookStatus}', ожидался '{BookStatus.Available}'",
                    _timeProvider.GetUtcNow().UtcDateTime));
                continue;
            }

            if (candidate.HasActiveBorrow)
            {
                skippedCount++;
                logs.Add(CreateBookLog(runId, candidate, "skipped", "Книга выдана читателю", _timeProvider.GetUtcNow().UtcDateTime));
                continue;
            }

            var hasActiveBorrow = await _bookArchiveRepository.HasActiveBorrow(candidate.BookId, cancellationToken);

            if (hasActiveBorrow)
            {
                skippedCount++;
                logs.Add(CreateBookLog(runId, candidate, "skipped", "Книга выдана читателю на момент обработки", _timeProvider.GetUtcNow().UtcDateTime));
                continue;
            }

            try
            {
                await _bookService.ArchiveBook(candidate.BookId);
                archivedCount++;
                logs.Add(CreateBookLog(runId, candidate, "archived", null, _timeProvider.GetUtcNow().UtcDateTime));
            }
            catch (Exception exception)
            {
                failedCount++;
                logs.Add(CreateBookLog(runId, candidate, "failed", exception.Message, _timeProvider.GetUtcNow().UtcDateTime));
            }
        }

        var completedAt = _timeProvider.GetUtcNow().UtcDateTime;
        var duration = completedAt - startTime;

        logs.Add(new ArchiveBookLogEntry
        {
            JobRunId = runId,
            BookId = null,
            BookTitle = null,
            Status = "summary",
            Reason = $"processed={candidates.Count}; archived={archivedCount}; skipped={skippedCount}; failed={failedCount}; durationMs={duration.TotalMilliseconds:F0}",
            ProcessedAt = completedAt
        });

        await _bookArchiveRepository.AddArchiveLogs(logs, cancellationToken);

        _logger.LogInformation(
            "{JobName}: processed={Processed}; archived={Archived}; skipped={Skipped}; failed={Failed}; duration={Duration}",
            JobName,
            candidates.Count,
            archivedCount,
            skippedCount,
            failedCount,
            duration);
    }

    private static ArchiveBookLogEntry CreateBookLog(
        Guid runId,
        ArchiveBookCandidate candidate,
        string status,
        string reason,
        DateTime processedAt)
    {
        return new ArchiveBookLogEntry
        {
            JobRunId = runId,
            BookId = candidate.BookId,
            BookTitle = candidate.BookTitle,
            Status = status,
            Reason = reason,
            ProcessedAt = processedAt
        };
    }
}
