using PracticalWork.Library.Contracts.v1.Reports.Request;
using PracticalWork.Library.Contracts.v1.Reports.Response;
using PracticalWork.Library.DTO.ActivityLogDtos;
using PracticalWork.Library.DTO.BaseDtos;
using PracticalWork.Library.Models.ReportModels;

namespace PracticalWork.Library.Controllers.Mappers.v1;

/// <summary>
/// Расширения для работы с объектами пагинации
/// </summary>
public static class PaginationExtensions
{
    /// <summary>
    /// Преобразует объект запроса пагинации из версии v1 контракта в DTO пагинации
    /// </summary>
    /// <param name="request">Объект запроса пагинации</param>
    /// <returns>DTO для пагинации</returns>
    public static PaginationRequestDto ToPaginationRequest(this Contracts.v1.Abstracts.PaginationRequest request) =>
        new()
        {
            PageSize = request.PageSize,
            PageNumber = request.PageNumber,
        };

    /// <summary>
    /// Преобразует объект запроса журналов активности в модель запроса с фильтрацией и пагинацией
    /// </summary>
    /// <param name="request">Исходный объект запроса</param>
    /// <returns>Модель запроса ActivityLogs с фильтрацией по дате и типам событий</returns>

    public static GetActivityLogsRequestModel
        ToGetActivityLogsRequestModel(this ActivityLogsPaginationRequest request) =>
        new()
        {
            PageSize = request.PageSize,
            PageNumber = request.PageNumber,
            EventTypes = request.EventTypes,
            EventDateFrom = request.EventDateFrom,
            EventDateTo = request.EventDateTo,
        };

    /// <summary>
    /// Преобразует объект пагинации активности в контракт
    /// </summary>
    public static Contracts.v1.Abstracts.PaginationResponse<ActivityLogResponse> ToActivityLogsPaginationResponse(
        this PaginationResponseDto<ActivityLog> paginationResponse) =>
        new()
        {
            Entities = paginationResponse.Entities
                .Select(e => new ActivityLogResponse(e.Event, e.EventType, e.EventDate))
                .ToList(),
            PageCount = paginationResponse.PageCount,
            TotalCount = paginationResponse.TotalCount,
            PageSize = paginationResponse.PageSize,
            PageNumber = paginationResponse.PageNumber,
        };
}