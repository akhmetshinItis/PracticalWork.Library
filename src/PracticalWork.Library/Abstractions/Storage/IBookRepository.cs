using PracticalWork.Library.DTO.BaseDtos;
using PracticalWork.Library.DTO.BookDtos;
using PracticalWork.Library.Models;
using PracticalWork.Library.Models.BookModels;

namespace PracticalWork.Library.Abstractions.Storage;

public interface IBookRepository
{
    /// <summary>
    /// Создать книгу
    /// </summary>
    /// <param name="book">Модель книги</param>
    /// <returns>Идентификатор созданной книги</returns>
    Task<Guid> CreateBook(Book book);
    
    /// <summary>
    /// Отредактировать книгу
    /// </summary>
    /// <param name="book">Модель для редактирования книги</param>
    /// <param name="bookId">Идентификатор книги</param>
    /// <returns>-</returns>
    Task UpdateBook(Book book, Guid bookId);
    
    /// <summary>
    /// Получить книгу по идентификатору
    /// </summary>
    /// <param name="bookId"></param>
    /// <returns></returns>
    Task<Book> GetBookById(Guid bookId);

    /// <summary>
    /// Получить список книг с фильтрами
    /// </summary>
    /// <param name="request">Запрос</param>
    /// <returns>Список книг</returns>
    Task<List<Book>> GetBooks(GetBooksRequestModel request);
    /// <summary>
    /// Получить не архивные книги с записями о выдаче
    /// </summary>
    /// <param name="request">объект пагинации</param>
    /// <returns>список книг</returns>
    Task<IReadOnlyList<Book>> GetNonArchivedBooksPageWithIssuanceRecords(
        PaginationRequestDto request);
    /// <summary>
    /// Получить книгу по названию
    /// </summary>
    /// <param name="title">название книги</param>
    /// <returns>идентификатор книги</returns>
    Task<Guid> GetBookIdByTitle(string title);
}