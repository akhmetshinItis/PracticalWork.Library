using PracticalWork.Library.DTO.BaseDtos;
using PracticalWork.Library.Enums;

namespace PracticalWork.Library.DTO.BookDtos
{
    /// <summary>
    /// Модель запроса для получения списка книг с поддержкой фильтрации и пагинации
    /// </summary>
    public class GetBooksRequestModel : PaginationRequestDto
    {
        /// <summary>
        /// Статус
        /// </summary>
        public BookStatus? Status { get; set; }
        
        /// <summary>
        /// Категория
        /// </summary>
        public BookCategory? Category { get; set; }
        
        /// <summary>
        /// Автор
        /// </summary>
        public string Author { get; set; }
    }
}