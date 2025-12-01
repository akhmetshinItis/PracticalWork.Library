using PracticalWork.Library.Contracts.v1.Enums;

namespace PracticalWork.Library.Contracts.v1.Books.Response
{
    public class BookResponse
    {
        /// <summary>
        /// Идентификатор книги
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// Наименование книги
        /// </summary>
        public string Title { get; init; } = string.Empty;
        
        /// <summary>
        /// Список авторов
        /// </summary>
        public IReadOnlyList<string> Authors { get; init; } = default!;
        
        /// <summary>
        /// Описание книги
        /// </summary>
        public string Description { get; init; }
        
        /// <summary>
        /// Год издания
        /// </summary>
        public int Year { get; init; }
        
        /// <summary>
        /// Статус книги
        /// </summary>
        public BookStatus Status { get; init; }
        
        /// <summary>
        /// Категория книги
        /// </summary>
        public BookCategory Category { get; init; }
        
        /// <summary>
        /// Путь до изображения с обложки
        /// </summary>
        public string CoverImagePath { get; init; } = default!;
    }
}