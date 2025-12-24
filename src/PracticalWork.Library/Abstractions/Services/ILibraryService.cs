using PracticalWork.Library.DTO.BaseDtos;
using PracticalWork.Library.Models.BookModels;

namespace PracticalWork.Library.Abstractions.Services;

public interface ILibraryService
{
    Task BorrowBook(Guid bookId, Guid readerId);
    Task ReturnBook(Guid bookId, Guid readerId);
    Task<(Guid bookId, Book book)> GetBookDetails(Guid bookId);
    Task<(Guid bookId, Book book)> GetBookDetails(string title);
    Task<PaginationResponseDto<Book>> GetNonArchivedBooksPage(PaginationRequestDto request);
}