namespace PracticalWork.Library.Models.BookModels;

/// <summary>
/// Запись лога архивации книги.
/// </summary>
public sealed class ArchiveBookLogEntry
{
    /// <summary>
    /// Идентификатор запуска задачи.
    /// </summary>
    public Guid JobRunId { get; init; }

    /// <summary>
    /// Идентификатор книги, если лог относится к конкретной книге.
    /// </summary>
    public Guid? BookId { get; init; }

    /// <summary>
    /// Название книги.
    /// </summary>
    public string BookTitle { get; init; }

    /// <summary>
    /// Итог обработки.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Причина пропуска или текст ошибки.
    /// </summary>
    public string Reason { get; init; }

    /// <summary>
    /// Время обработки.
    /// </summary>
    public DateTime ProcessedAt { get; init; }
}
