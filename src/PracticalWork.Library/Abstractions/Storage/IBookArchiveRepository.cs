using PracticalWork.Library.Models.BookModels;

namespace PracticalWork.Library.Abstractions.Storage;

/// <summary>
/// Репозиторий данных для архивации книг.
/// </summary>
public interface IBookArchiveRepository
{
    /// <summary>
    /// Получить кандидатов на архивацию.
    /// </summary>
    /// <param name="thresholdDate">Пороговая дата последней выдачи.</param>
    /// <param name="maxBooks">Максимальное количество книг за запуск.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Список кандидатов на архивацию.</returns>
    Task<IReadOnlyList<ArchiveBookCandidate>> GetArchiveCandidates(
        DateOnly thresholdDate,
        int maxBooks,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверить, выдана ли книга читателю.
    /// </summary>
    /// <param name="bookId">Идентификатор книги.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Признак активной выдачи.</returns>
    Task<bool> HasActiveBorrow(Guid bookId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохранить результат запуска архивации и логи по книгам.
    /// </summary>
    /// <param name="jobRun">Сводка запуска архивации.</param>
    /// <param name="logs">Логи обработки.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task SaveArchiveProcessingResult(
        ArchiveJobRun jobRun,
        IReadOnlyCollection<ArchiveBookLogEntry> logs,
        CancellationToken cancellationToken = default);
}
