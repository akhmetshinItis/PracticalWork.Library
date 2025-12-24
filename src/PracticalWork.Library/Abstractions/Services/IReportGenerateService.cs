using PracticalWork.Library.Models.ReportModels;

namespace PracticalWork.Library.Abstractions.Services;

/// <summary>
/// Сервис отвечает за генерацию отчетов
/// </summary>
public interface IReportGenerateService
{
    /// <summary>
    /// Генерирует отчет по записям событий системы
    /// </summary>
    /// <param name="reportId">Индентификатор отчета</param>
    /// <param name="logs">Записи событий системы</param>
    /// <returns>Объект сгенерированного отчета</returns>
    ReportGenerateResult GenerateReport(Guid reportId, IReadOnlyList<ActivityLog> logs);
}