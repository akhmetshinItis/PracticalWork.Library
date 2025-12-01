using PracticalWork.Library.Enums;
using PracticalWork.Library.Models.BaseModels;

namespace PracticalWork.Library.Models.BookModels
{
    public class GetBooksRequestModel : PaginationRequestBase
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