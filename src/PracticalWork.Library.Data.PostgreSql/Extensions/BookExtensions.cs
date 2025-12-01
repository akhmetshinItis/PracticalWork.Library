using PracticalWork.Library.Data.PostgreSql.Entities;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Models.BookModels;

namespace PracticalWork.Library.Data.PostgreSql.Extensions
{
    /// <summary>
    /// Методы расширения для <see cref="AbstractBookEntity"/>
    /// </summary>
    public static class BookExtensions
    {
        /// <summary>
        /// Привести к <see cref="Book"/>
        /// </summary>
        /// <param name="bookEntity">Базовый класс для книг</param>
        /// <returns>Книга</returns>
        public static Book ToBook(this AbstractBookEntity bookEntity)
            => new()
            {
                Id = bookEntity.Id,
                Title = bookEntity.Title,
                Description = bookEntity.Description,
                Authors = bookEntity.Authors,
                Year = bookEntity.Year,
                Category = bookEntity.Category,
                Status = bookEntity.Status,
                CoverImagePath = bookEntity.CoverImagePath,
                IsArchived = bookEntity.Status == BookStatus.Archived,
            };
    }
}