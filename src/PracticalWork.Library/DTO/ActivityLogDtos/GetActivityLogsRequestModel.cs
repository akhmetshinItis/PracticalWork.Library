using PracticalWork.Library.DTO.BaseDtos;

namespace PracticalWork.Library.DTO.ActivityLogDtos;

public class GetActivityLogsRequestModel: PaginationRequestDto
{
    public DateOnly? EventDateFrom { get; set; } 
    public DateOnly? EventDateTo { get; set; }
    public string[] EventTypes { get; set; }
}