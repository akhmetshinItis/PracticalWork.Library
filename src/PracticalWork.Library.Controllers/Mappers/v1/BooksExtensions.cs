using PracticalWork.Library.Contracts.v1.Books.Request;
using PracticalWork.Library.Contracts.v1.Books.Response;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Models.BaseModels;
using PracticalWork.Library.Models.BookModels;

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
            Category = (BookCategory)request.Category,
            Status = (BookStatus)request.Status,
            PageSize = request.PageSize,
            PageNumber = request.PageNumber,
        };

    public static Contracts.v1.Abstracts.PaginationResponseBase<BookResponse> ToResponseBookContract(
        this PaginationResponseBase<Book> booksPaginationResponse)
        => new()
        {
            Entities = booksPaginationResponse.Entities.Select(x => new BookResponse
            {
                Id = x.Id,
                Authors = x.Authors,
                Category = (Contracts.v1.Enums.BookCategory) x.Category,
                CoverImagePath = x.CoverImagePath,
                Description = x.Description,
                Status = (Contracts.v1.Enums.BookStatus) x.Status,
                Title = x.Title,
                Year = x.Year,
            })
            .ToList(),
        };
}