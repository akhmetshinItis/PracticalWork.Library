using Microsoft.Extensions.Options;
using PracticalWork.Library.Abstractions.MessageBroker;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.DTO.ActivityLogDtos;
using PracticalWork.Library.DTO.BaseDtos;
using PracticalWork.Library.DTO.ReportDtos;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Events;
using PracticalWork.Library.Helpers;
using PracticalWork.Library.Models.ReportModels;
using PracticalWork.Library.Options;

namespace PracticalWork.Library.Services;

public class ReportService: IReportService
{
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly IReportRepository _reportRepository;
    private readonly IMessageProducer _producer;
    private readonly IMinioService _minioService;
    private readonly ICacheService _cacheService;
    private readonly ICacheVersionService _cacheVersionService;
    private readonly MinioOptions _minioOptions;
    private readonly BooksCacheOptions _cacheOptions;
    
    public ReportService(IActivityLogRepository activityLogRepository,
        IReportRepository reportRepository,
        IMessageProducer producer,
        ICacheService cacheService,
        IMinioService minioService,
        IOptionsMonitor<MinioOptions> minioOptions,
        ICacheVersionService cacheVersionService, 
        IOptionsMonitor<BooksCacheOptions> cacheOptions)
    {
        _activityLogRepository = activityLogRepository;
        _reportRepository = reportRepository;
        _cacheService = cacheService;
        _producer = producer;
        _minioService = minioService;
        _minioOptions = minioOptions.CurrentValue;
        _cacheVersionService = cacheVersionService;
        _cacheOptions = cacheOptions.CurrentValue;
    }

    public async Task WriteSystemActivityLogs(ActivityLog log)
    {
        await _activityLogRepository.AddLogAsync(log);
    }

    public async Task<PaginationResponseDto<ActivityLog>> ReadSystemActivityLogs(GetActivityLogsRequestModel model)
    {
        var logs = await _activityLogRepository
            .GetLogsPageAsync(model);
        return new PaginationResponseDto<ActivityLog>()
        {
            Entities = logs.Item1,
            TotalCount = logs.Item2,
            PageNumber = model.PageNumber,
            PageSize = model.PageSize,
        };
    }

    public async Task<Report> CreateReport(DateOnly? eventDateFrom, DateOnly? eventDateTo, string[] eventTypes)
    {
        var report = new Report
        {
            PeriodFrom = eventDateFrom,
            PeriodTo = eventDateTo,
            EventTypes = eventTypes,
        };
        var id = await _reportRepository.CreateReport(report);
        var message = new ReportCreateEvent(id, eventDateFrom, eventDateTo, eventTypes,report.Status);
        await _producer.ProduceReportCreateAsync(message);
        await CacheManager.InvalidateReportsCacheAsync(_cacheVersionService, _cacheOptions);
        return report;
    }

    public async Task<IReadOnlyList<Report>> GetListOfReadyReports()
    {
        var cacheCheckResult = await CacheManager.CheckCacheAsync<Report,ReportDto>(
            _cacheVersionService,_cacheService,
            _cacheOptions.ReportsCacheOptions.Prefix, "ready",
            dto => new Report
            {
                Name = dto.Name,
                EventTypes = dto.EventTypes,
                FilePath = dto.FilePath,
                GeneratedAt = dto.GeneratedAt,
                PeriodFrom = dto.PeriodFrom,
                PeriodTo = dto.PeriodTo,
                Status = dto.Status,
            });
        if (cacheCheckResult.Count != 0)
        {
            return cacheCheckResult;
        }
        
        var reports = await _reportRepository.GetReadyReports();

        await CacheManager.WriteToCacheAsync(
            _cacheVersionService, _cacheService,
            _cacheOptions.ReportsCacheOptions, "ready", reports,
            model => new ReportDto
            {
                Name = model.Name,
                EventTypes = model.EventTypes,
                FilePath = model.FilePath,
                GeneratedAt = model.GeneratedAt,
                PeriodFrom = model.PeriodFrom,
                PeriodTo = model.PeriodTo,
                Status = model.Status,
            });
        return reports;
    }

    public async Task<string> GetReportUrl(string reportName)
    {
        var (id,report) = await _reportRepository.GetReportByName(reportName);
        var generatedDate = report.GeneratedAt ?? DateTime.UtcNow;
        var fileName = $"{generatedDate.Year}/{generatedDate.Month}/{reportName}";
        var filePath = await _minioService.GetFileUrlAsync(
            _minioOptions.ReportsBucketName, fileName);
        report.FilePath = filePath;
        await _reportRepository.UpdateReport(id,report);
        await CacheManager.InvalidateReportsCacheAsync(_cacheVersionService, _cacheOptions);
        return filePath;
    }
}
