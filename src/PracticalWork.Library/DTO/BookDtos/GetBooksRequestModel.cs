using PracticalWork.Library.DTO.BaseDtos;
using PracticalWork.Library.Enums;

namespace PracticalWork.Library.DTO.BookDtos
{
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