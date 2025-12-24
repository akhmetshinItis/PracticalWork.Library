using PracticalWork.Library.DTO.ActivityLogDtos;
using PracticalWork.Library.DTO.BaseDtos;
using PracticalWork.Library.Models.ReportModels;

namespace PracticalWork.Library.Abstractions.Services;

/// <summary>
/// Сервис управления отчетами и записями событий системы
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Записать лог
    /// </summary>
    /// <param name="log">Запись события системы</param>
    /// <returns>задача</returns>
    Task WriteSystemActivityLogs(ActivityLog log);

    /// <summary>
    /// Прочитать страницу с записями событий системы
    /// </summary>
    /// <param name="model">Объект запроса пагинации</param>
    /// <returns>Объект пагинации с записями</returns>
    Task<PaginationResponseDto<ActivityLog>> ReadSystemActivityLogs(GetActivityLogsRequestModel model);
    
    /// <summary>
    /// Создать отчет по событиям системы
    /// </summary>
    /// <param name="eventDateFrom">Фильтр на дату начала событий системы</param>
    /// <param name="eventDateTo">Фильтр на дату окончания событий системы</param>
    /// <param name="eventTypes">Фильтр на типы событий</param>
    /// <returns>Отчет со статусом "в процессе"</returns>
    Task<Report> CreateReport(DateOnly? eventDateFrom, DateOnly? eventDateTo, string[] eventTypes);
    
    /// <summary>
    /// Сгенерировать отчет
    /// </summary>
    /// <param name="reportId">Идентификатор отчета</param>
    /// <param name="periodFrom">Фильтр на дату начала событий системы</param>
    /// <param name="periodTo">Фильтр на дату окончания событий системы</param>
    /// <param name="eventTypes">Фильтр на типы событий</param>
    /// <returns>задача</returns>
    Task GenerateReport(Guid reportId, DateOnly? periodFrom,
        DateOnly? periodTo, string[] eventTypes);
    
    /// <summary>
    /// Получить список готовых отчетов
    /// </summary>
    /// <returns>Список готовых отчетов</returns>
    Task<IReadOnlyList<Report>> GetListOfReadyReports();
    
    /// <summary>
    /// Получить ссылку на отчет
    /// </summary>
    /// <param name="reportName">Название файла отчета</param>
    /// <returns>url файла</returns>
    Task<string> GetReportUrl(string reportName);
}