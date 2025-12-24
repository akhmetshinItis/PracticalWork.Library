using PracticalWork.Library.Contracts.v1.Reports.Response;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Models.ReportModels;

namespace PracticalWork.Library.Controllers.Mappers.v1;

/// <summary>
/// Расширения для преобразования объектов отчетов
/// </summary>
public static class ReportsExtensions
{
    /// <summary>
    /// Преобразует модель Report в DTO ответа с деталями отчета
    /// </summary>
    /// <param name="report">Модель отчета</param>
    /// <returns>DTO ответа ReportResponse</returns>
    public static ReportResponse ToReportResponse(this Report report)
    {
        return new ReportResponse(
            report.Name, 
            report.FilePath, 
            report.PeriodFrom, 
            report.PeriodTo, 
            report.EventTypes, 
            report.GeneratedAt);
    }

    /// <summary>
    /// Преобразует модель Report в DTO ответа после создания отчета
    /// </summary>
    /// <param name="report">Модель отчета</param>
    /// <returns>DTO ответа ReportCreateResponse</returns>
    public static ReportCreateResponse ToReportCreateResponse(this Report report) =>
        new()
        {
            EventTypes = report.EventTypes,
            PeriodFrom = report.PeriodFrom,
            PeriodTo = report.PeriodTo,
            Status = report.Status,
        };
}

/// <summary>
/// DTO ответа после создания отчета
/// </summary>
public class ReportCreateResponse
{
    /// <summary>
    /// Начало отчетного периода
    /// </summary>
    public DateOnly? PeriodFrom { get; set; }

    /// <summary>
    /// Конец отчетного периода
    /// </summary>
    public DateOnly? PeriodTo { get; set; }

    /// <summary>
    /// Список типов событий, включенных в отчет
    /// </summary>
    public IReadOnlyList<string> EventTypes { get; set; }

    /// <summary>
    /// Текущий статус отчета
    /// </summary>
    public ReportStatus Status { get; set; }
}
