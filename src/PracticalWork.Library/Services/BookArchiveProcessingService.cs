using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Models.BookModels;

namespace PracticalWork.Library.Services;

/// <summary>
/// Сервис сценария архивации старых книг.
/// </summary>
public sealed class BookArchiveProcessingService : IBookArchiveProcessingService
{
    private readonly IBookArchiveRepository _bookArchiveRepository;
    private readonly IBookService _bookService;
    private readonly TimeProvider _timeProvider;

    public BookArchiveProcessingService(
        IBookArchiveRepository bookArchiveRepository,
        IBookService bookService,
        TimeProvider timeProvider)
    {
        _bookArchiveRepository = bookArchiveRepository;
        _bookService = bookService;
        _timeProvider = timeProvider;
    }

    public async Task<ArchiveBooksProcessingResult> ProcessAsync(
        DateOnly thresholdDate,
        int maxBooks,
        CancellationToken cancellationToken = default)
    {
        var candidates = await _bookArchiveRepository.GetArchiveCandidates(
            thresholdDate,
            maxBooks,
            cancellationToken);

        if (candidates.Count == 0)
        {
            return new ArchiveBooksProcessingResult
            {
                ProcessedCount = 0,
                Duration = TimeSpan.Zero
            };
        }

        var runId = Guid.NewGuid();
        var startTime = _timeProvider.GetUtcNow().UtcDateTime;
        var archivedCount = 0;
        var skippedCount = 0;
        var failedCount = 0;
        var logs = new List<ArchiveBookLogEntry>();

        foreach (var candidate in candidates)
        {
            var result = await ProcessCandidateAsync(runId, candidate, cancellationToken);

            archivedCount += result.ArchivedCount;
            skippedCount += result.SkippedCount;
            failedCount += result.FailedCount;
            logs.Add(result.LogEntry);
        }

        var completedAt = _timeProvider.GetUtcNow().UtcDateTime;
        var duration = completedAt - startTime;

        logs.Add(CreateSummaryLog(
            runId,
            candidates.Count,
            archivedCount,
            skippedCount,
            failedCount,
            duration,
            completedAt));

        await _bookArchiveRepository.AddArchiveLogs(logs, cancellationToken);

        return new ArchiveBooksProcessingResult
        {
            ProcessedCount = candidates.Count,
            ArchivedCount = archivedCount,
            SkippedCount = skippedCount,
            FailedCount = failedCount,
            Duration = duration
        };
    }

    private async Task<ArchiveCandidateProcessingResult> ProcessCandidateAsync(
        Guid runId,
        ArchiveBookCandidate candidate,
        CancellationToken cancellationToken)
    {
        if (candidate.BookStatus != BookStatus.Available)
        {
            return CreateCandidateResult(
                runId,
                candidate,
                "skipped",
                $"Текущий статус книги '{candidate.BookStatus}', ожидался '{BookStatus.Available}'",
                skippedCount: 1);
        }

        if (candidate.HasActiveBorrow)
        {
            return CreateCandidateResult(
                runId,
                candidate,
                "skipped",
                "Книга выдана читателю",
                skippedCount: 1);
        }

        var hasActiveBorrow = await _bookArchiveRepository.HasActiveBorrow(candidate.BookId, cancellationToken);
        if (hasActiveBorrow)
        {
            return CreateCandidateResult(
                runId,
                candidate,
                "skipped",
                "Книга выдана читателю на момент обработки",
                skippedCount: 1);
        }

        try
        {
            await _bookService.ArchiveBook(candidate.BookId);
            return CreateCandidateResult(
                runId,
                candidate,
                "archived",
                null,
                archivedCount: 1);
        }
        catch (Exception exception)
        {
            return CreateCandidateResult(
                runId,
                candidate,
                "failed",
                exception.Message,
                failedCount: 1);
        }
    }

    private ArchiveCandidateProcessingResult CreateCandidateResult(
        Guid runId,
        ArchiveBookCandidate candidate,
        string status,
        string reason,
        int archivedCount = 0,
        int skippedCount = 0,
        int failedCount = 0)
    {
        return new ArchiveCandidateProcessingResult
        {
            ArchivedCount = archivedCount,
            SkippedCount = skippedCount,
            FailedCount = failedCount,
            LogEntry = new ArchiveBookLogEntry
            {
                JobRunId = runId,
                BookId = candidate.BookId,
                BookTitle = candidate.BookTitle,
                Status = status,
                Reason = reason,
                ProcessedAt = _timeProvider.GetUtcNow().UtcDateTime
            }
        };
    }

    private static ArchiveBookLogEntry CreateSummaryLog(
        Guid runId,
        int processedCount,
        int archivedCount,
        int skippedCount,
        int failedCount,
        TimeSpan duration,
        DateTime processedAt)
    {
        return new ArchiveBookLogEntry
        {
            JobRunId = runId,
            BookId = null,
            BookTitle = null,
            Status = "summary",
            Reason = $"processed={processedCount}; archived={archivedCount}; skipped={skippedCount}; failed={failedCount}; durationMs={duration.TotalMilliseconds:F0}",
            ProcessedAt = processedAt
        };
    }

    private sealed class ArchiveCandidateProcessingResult
    {
        public int ArchivedCount { get; init; }
        public int SkippedCount { get; init; }
        public int FailedCount { get; init; }
        public required ArchiveBookLogEntry LogEntry { get; init; }
    }
}
