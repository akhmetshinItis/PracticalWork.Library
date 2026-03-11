namespace PracticalWork.Reports.Worker.Abstractions;

public interface IReportGenerationService
{
    Task GenerateReportAsync(
        Guid reportId,
        DateOnly? periodFrom,
        DateOnly? periodTo,
        string[] eventTypes,
        CancellationToken cancellationToken);
}
