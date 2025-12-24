
using PracticalWork.Library.Contracts.v1.Abstracts;

namespace PracticalWork.Library.Contracts.v1.Reports.Request;

public class ActivityLogsPaginationRequest : PaginationRequestBase
{
    public DateOnly? EventDateFrom { get; set; } 
    public DateOnly? EventDateTo { get; set; }
    public string[] EventTypes { get; set; }
}