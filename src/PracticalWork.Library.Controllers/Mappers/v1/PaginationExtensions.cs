using PracticalWork.Library.Contracts.v1.Reports.Request;
using PracticalWork.Library.Contracts.v1.Reports.Response;
using PracticalWork.Library.DTO.ActivityLogDtos;
using PracticalWork.Library.DTO.BaseDtos;
using PracticalWork.Library.Models.ReportModels;

namespace PracticalWork.Library.Controllers.Mappers.v1;

public static class PaginationExtensions
{
    public static PaginationRequestDto ToPaginationRequest(this Contracts.v1.Abstracts.PaginationRequest request) =>
        new()
        {
            PageSize = request.PageSize,
            PageNumber = request.PageNumber,
        };

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

    public static Contracts.v1.Abstracts.PaginationResponse<ActivityLogResponse> ToActivityLogsPaginationResponse(
        this PaginationResponseDto<ActivityLog> paginationResponse) =>
        new()
        {
            Entities = paginationResponse.Entities
                .Select(e => new ActivityLogResponse(e.Event, e.EventType, e.EventDate))
                .ToList(),
        };
}