using PracticalWork.Library.DTO.BaseDtos;

namespace PracticalWork.Library.Controllers.Mappers.v1;

public static class PaginationExtensions
{
    public static PaginationRequestBase ToPaginationRequest(this Contracts.v1.Abstracts.PaginationRequestBase request) =>
        new()
        {
            PageSize = request.PageSize,
            PageNumber = request.PageNumber,
        };
}