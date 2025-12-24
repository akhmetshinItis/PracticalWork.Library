using PracticalWork.Library.Models;
using PracticalWork.Library.Models.ReportModels;

namespace PracticalWork.Library.Abstractions.Storage;

/// <summary>
/// Репозиторий получения данных об отчетах
/// </summary>
public interface IReportRepository
{
    /// <summary>
    /// Создать отчет
    /// </summary>
    /// <param name="report">Объект отчета</param>
    /// <returns>Идентификатор отчета</returns>
    Task<Guid> CreateReport(Report report);
    
    /// <summary>
    /// Получить готовые отчеты
    /// </summary>
    /// <returns>Список отчетов</returns>
    Task<IReadOnlyList<Report>> GetReadyReports();
    
    /// <summary>
    /// Получить отчет по идентификатору
    /// </summary>
    /// <param name="reportId">Идентификатор отчета</param>
    /// <returns>Объект отчета</returns>
    Task<Report> GetReportById(Guid reportId);
    
    /// <summary>
    /// Получить отчет по имени файла
    /// </summary>
    /// <param name="reportName">Название отчета</param>
    /// <returns>Идентификатор и объект отчета</returns>
    Task<(Guid id, Report report)> GetReportByName(string reportName);
    
    /// <summary>
    /// Обновить информациб об отчете
    /// </summary>
    /// <param name="reportId">Идентификатор отчета</param>
    /// <param name="report">Объект отчета</param>
    /// <returns>Задача</returns>
    Task UpdateReport(Guid reportId,Report report);
}