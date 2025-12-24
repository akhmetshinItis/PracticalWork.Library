using PracticalWork.Library.DTO.BaseDtos;

namespace PracticalWork.Library.DTO.ActivityLogDtos;

/// <summary>
/// Модель запроса для получения журналов активности с поддержкой пагинации и фильтрации
/// </summary>
public class GetActivityLogsRequestModel : PaginationRequestDto
{
    /// <summary>
    /// Начальная дата события для фильтрации
    /// </summary>
    public DateOnly? EventDateFrom { get; set; } 

    /// <summary>
    /// Конечная дата события для фильтрации
    /// </summary>
    public DateOnly? EventDateTo { get; set; }

    /// <summary>
    /// Массив типов событий для фильтрации
    /// </summary>
    public string[] EventTypes { get; set; }
}
