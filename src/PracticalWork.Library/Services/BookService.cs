using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Exceptions;
using PracticalWork.Library.Models.BaseModels;
using PracticalWork.Library.Models.BookModels;

namespace PracticalWork.Library.Services;

public sealed class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;

    /// <summary>
    /// Конструктор
    /// </summary>
    /// <param name="bookRepository">Репозиторий работы с книгами</param>
    public BookService(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }
    
    /// <inheritdoc/> 
    public async Task<Guid> CreateBook(Book book)
    {
        book.Status = BookStatus.Available;
        try
        {
            return await _bookRepository.CreateBook(book);
        }
        catch (Exception ex)
        {
            throw new BookServiceException("Ошибка создание книги!", ex);
        }
    }

    /// <inheritdoc/> 
    public async Task UpdateBook(Book model, Guid id)
    {
        var book = await _bookRepository.GetBookById(id);

        if (book.IsArchived)
        {
            throw new BookServiceException("Книга в архиве");
        }

        book.Update(model);
        
        await _bookRepository.UpdateBook(book, id);
    }

    /// <inheritdoc/> 
    public async Task ArchiveBook(Guid id)
    {
       var book = await _bookRepository.GetBookById(id);
       book.Archive();
       
       await _bookRepository.UpdateBook(book, id);
    }

    /// <inheritdoc />
    public async Task<PaginationResponseBase<Book>> GetBooks(GetBooksRequestModel requestModel)
    {
        var books = await _bookRepository.GetBooks(requestModel);

        return new PaginationResponseBase<Book>
        {
            Entities = books,
        };
    }
    
    
    public async Task UpdateBookDetails(Guid id, string description, UploadedFile coverFile)
    {
        var book = await _bookRepository.GetBookById(id);

        if (book.IsArchived)
            throw new BookServiceException("Нельзя изменить архивную книгу");

        string coverPath = book.CoverImagePath;

        if (coverFile?.HasFile == true)
        {
            ValidateCover(coverFile);

            // coverPath = await _fileStorage.UploadBookCover(id, coverFile);
        }

        book.UpdateDetails(description, coverPath);

        await _bookRepository.UpdateBook(book, id);

        // await _cache.RemoveAsync($"book:{id}");
    }

    private void ValidateCover(UploadedFile file)
    {
        if (file.Stream!.Length > 5 * 1024 * 1024)
            throw new BookServiceException("Размер обложки превышает 5 МБ");

        if (file.ContentType is not ("image/jpeg" or "image/png"))
            throw new BookServiceException("Разрешён только JPEG или PNG");
    }

}