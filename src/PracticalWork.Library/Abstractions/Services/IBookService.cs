using PracticalWork.Library.Models;
using PracticalWork.Library.Models.BaseModels;
using PracticalWork.Library.Models.BookModels;

namespace PracticalWork.Library.Abstractions.Services;

public interface IBookService
{
    /// <summary>
    /// Создание книги
    /// </summary>
    /// <returns>Идентификатор созданной книги</returns>
    Task<Guid> CreateBook(Book book);

    /// <summary>
    /// Редактирование книги
    /// </summary>
    Task UpdateBook(Book model, Guid id);
    
    /// <summary>
    /// Перевести книгу в архив
    /// </summary>
    /// <param name="bookId">Идентификатор книги</param>
    Task ArchiveBook(Guid bookId);
    
    /// <summary>
    /// Получить список книг
    /// </summary>
    /// <returns></returns>
    Task<PaginationResponseBase<Book>> GetBooks(GetBooksRequestModel requestModel);
    
    
    Task UpdateBookDetails(Guid id, string description, UploadedFile coverFile);

}