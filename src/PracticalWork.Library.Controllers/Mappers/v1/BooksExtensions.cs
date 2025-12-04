using PracticalWork.Library.Contracts.v1.Books.Request;
using PracticalWork.Library.Contracts.v1.Books.Response;
using PracticalWork.Library.DTO.BaseDtos;
using PracticalWork.Library.DTO.BookDtos;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Models.BookModels;
using BookIssueStatus = PracticalWork.Library.Contracts.v1.Enums.BookIssueStatus;

namespace PracticalWork.Library.Controllers.Mappers.v1;

public static class BooksExtensions
{
    public static Book ToBook(this CreateBookRequest request) =>
        new()
        {
            Authors = request.Authors,
            Title = request.Title,
            Description = request.Description,
            Year = request.Year,
            Category = (BookCategory)request.Category
        };

    public static Book ToBook(this UpdateBookRequest request) =>
        new()
        {
            Authors = request.Authors,
            Title = request.Title,
            Description = request.Description,
            Year = request.Year,
        };

    public static GetBooksRequestModel ToRequestBookModel(this GetBooksRequest request)
        => new()
        {
            Author = request.Author,
            Category = request.Category == null ? null: (BookCategory)request.Category,
            Status = request.Status == null ? null : (BookStatus)request.Status,
            PageSize = request.PageSize,
            PageNumber = request.PageNumber,
        };

    public static Contracts.v1.Abstracts.PaginationResponseBase<BookResponse> ToBookPaginationResponse(
        this PaginationResponseBase<Book> booksPaginationResponse)
        => new()
        {
            Entities = booksPaginationResponse.Entities
                .Select(ToBookResponse)
                .ToList(),
        };
    public static BookResponse ToBookResponse(this Book book) =>
        new BookResponse(
            book.Title, 
            (Contracts.v1.Enums.BookCategory)book.Category, 
            book.Authors, book.Description, 
            book.Year, 
            (Contracts.v1.Enums.BookStatus)book.Status, 
            book.IsArchived);
    public static ArchiveBookResponse ToArchiveBookResponse(this BookArchive book) =>
        new(book.Id, book.Title, book.ArchivedAt);
    public static BookDetailsResponse ToBookDetailsResponse(this Book book, Guid id) =>
        new (
            id,
            book.Title,
            (Contracts.v1.Enums.BookCategory)book.Category,
            book.Authors,
            book.Description,
            book.Year,
            book.CoverImagePath,
            (Contracts.v1.Enums.BookStatus)book.Status,
            book.IsArchived
        );

    public static Contracts.v1.Abstracts.PaginationResponseBase<BookWithIssuanceRecordsResponse>
        ToBookWithIssuanceCursorPaginationResponse(this PaginationResponseBase<Book> response) =>
        new()
        {
            Entities = response.Entities.Select(ToBookWithIssuanceRecordsResponse).ToList(),
        };
    public static BookWithIssuanceRecordsResponse ToBookWithIssuanceRecordsResponse(this Book book) => 
        new BookWithIssuanceRecordsResponse(
            book.Title, 
            (Contracts.v1.Enums.BookCategory)book.Category, 
            book.Authors, book.Description, 
            book.Year, 
            (Contracts.v1.Enums.BookStatus)book.Status, 
            book.IsArchived,
            book.IssuanceRecords
                .Select(i => i.ToIssuanceRecord())
                .ToList()
        );
    public static IssuanceRecord ToIssuanceRecord(this BookBorrow book) =>
        new((BookIssueStatus)book.Status, book.DueDate, book.ReturnDate, book.BorrowDate);
    public static BorrowedBookResponse ToBorrowedBookResponse(this BorrowedBook book) =>
        new (
            book.Title, 
            (Contracts.v1.Enums.BookCategory)book.Category, 
            book.Authors,
            book.Description, 
            book.Year, 
            (BookIssueStatus)book.Status, 
            book.DueDate, 
            book.ReturnDate, 
            book.BorrowDate);
}