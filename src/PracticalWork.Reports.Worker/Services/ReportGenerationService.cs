using Microsoft.Extensions.Options;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Helpers;
using PracticalWork.Library.Options;
using PracticalWork.Reports.Worker.Abstractions;

namespace PracticalWork.Reports.Worker.Services;

public sealed class ReportGenerationService : IReportGenerationService
{
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly IReportRepository _reportRepository;
    private readonly IMinioService _minioService;
    private readonly IReportGenerateService _reportGenerateService;
    private readonly ICacheVersionService _cacheVersionService;
    private readonly MinioOptions _minioOptions;
    private readonly BooksCacheOptions _cacheOptions;
    private readonly TimeProvider _timeProvider;

    public ReportGenerationService(
        IActivityLogRepository activityLogRepository,
        IReportRepository reportRepository,
        IMinioService minioService,
        IReportGenerateService reportGenerateService,
        ICacheVersionService cacheVersionService,
        IOptionsMonitor<MinioOptions> minioOptions,
        IOptionsMonitor<BooksCacheOptions> cacheOptions,
        TimeProvider timeProvider)
    {
        _activityLogRepository = activityLogRepository;
        _reportRepository = reportRepository;
        _minioService = minioService;
        _reportGenerateService = reportGenerateService;
        _cacheVersionService = cacheVersionService;
        _minioOptions = minioOptions.CurrentValue;
        _cacheOptions = cacheOptions.CurrentValue;
        _timeProvider = timeProvider;
    }

    public async Task GenerateReportAsync(
        Guid reportId,
        DateOnly? periodFrom,
        DateOnly? periodTo,
        string[] eventTypes,
        CancellationToken cancellationToken)
    {
        var report = await _reportRepository.GetReportById(reportId);
        var logs = await _activityLogRepository.GetLogsAsync(periodFrom, periodTo, eventTypes);

        try
        {
            var reportResult = _reportGenerateService.GenerateReport(reportId, logs.Item1);

            await _minioService.UploadFileAsync(
                _minioOptions.ReportsBucketName,
                reportResult.FileName,
                reportResult.Content,
                reportResult.ContentType);

            var fileName = reportResult.FileName.Split('/')[^1];
            report.MarkAsGenerated(fileName, _timeProvider);
            await _reportRepository.UpdateReport(reportId, report);
            await CacheManager.InvalidateReportsCacheAsync(_cacheVersionService, _cacheOptions);
        }
        catch (Exception)
        {
            report.Status = ReportStatus.Error;
            await _reportRepository.UpdateReport(reportId, report);
            await CacheManager.InvalidateReportsCacheAsync(_cacheVersionService, _cacheOptions);
            throw;
        }
    }
}
