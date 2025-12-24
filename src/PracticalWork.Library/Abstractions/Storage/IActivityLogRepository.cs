using PracticalWork.Library.DTO.ActivityLogDtos;
using PracticalWork.Library.DTO.BaseDtos;
using PracticalWork.Library.Models;
using PracticalWork.Library.Models.ReportModels;

namespace PracticalWork.Library.Abstractions.Storage;

/// <summary>
/// Репозиторий получения данных записей событий системы
/// </summary>
public interface IActivityLogRepository
{
    /// <summary>
    /// Добавить запись события
    /// </summary>
    /// <param name="activityLog">Объект события</param>
    /// <returns>Задача</returns>
    Task AddLogAsync(ActivityLog activityLog);

    /// <summary>
    /// Получить страницу записей событий
    /// </summary>
    /// <param name="model">Объект запроса пагинации</param>
    /// <returns>Список записей</returns>
    Task<(IReadOnlyList<ActivityLog>, int)> GetLogsPageAsync(GetActivityLogsRequestModel model);
    
    /// <summary>
    /// Получить записи событий
    /// </summary>
    /// <param name="periodFrom">Фильтр на начало даты</param>
    /// <param name="periodTo">Фильтр на окончание даты</param>
    /// <param name="eventTypes">Фильтр на типы событий</param>
    /// <returns>Список записей</returns>
    Task<(IReadOnlyList<ActivityLog>, int)> GetLogsAsync(
        DateOnly? periodFrom, DateOnly? periodTo, string[] eventTypes);
}