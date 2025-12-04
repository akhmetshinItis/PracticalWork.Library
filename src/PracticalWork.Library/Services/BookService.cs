using Microsoft.Extensions.Options;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.DTO.BaseDtos;
using PracticalWork.Library.DTO.BookDtos;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Exceptions;
using PracticalWork.Library.Helpers;
using PracticalWork.Library.Models.BookModels;
using PracticalWork.Library.Options;

namespace PracticalWork.Library.Services;

public sealed class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;
    private readonly ICacheService _cacheService;
    private readonly ICacheVersionService _cacheVersionService;
    private readonly BooksCacheOptions _cacheOptions;
    private readonly IMinioService _minioService;

    /// <summary>
    /// Конструктор
    /// </summary>
    /// <param name="bookRepository">Репозиторий работы с книгами</param>
    /// <param name="cacheService">Сервис кеша</param>
    /// <param name="minioService">Сервис минио</param>
    /// <param name="cacheVersionService">Сервис версионирования кеша</param>
    /// <param name="cacheOptions">Опции кеша для книг</param>
    public BookService(
        IBookRepository bookRepository,
        ICacheService cacheService,
        IMinioService minioService,
        ICacheVersionService cacheVersionService,
        IOptionsMonitor<BooksCacheOptions> cacheOptions)
    {
        _minioService = minioService;
        _bookRepository = bookRepository;
        _cacheService = cacheService;
        _cacheVersionService = cacheVersionService;
        _cacheOptions = cacheOptions.CurrentValue;
    }
    
    /// <inheritdoc/> 
    public async Task<Guid> CreateBook(Book book)
    {
        book.Status = BookStatus.Available;
        try
        {
            var bookId = await _bookRepository.CreateBook(book);
            
            return bookId;
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
        
        await BookCacheManager.InvalidateBookCacheAsync(_cacheVersionService,_cacheOptions);
    }

    /// <inheritdoc/> 
    public async Task<BookArchive> ArchiveBook(Guid id)
    {
       var book = await _bookRepository.GetBookById(id);
       book.Archive();
       
       await _bookRepository.UpdateBook(book, id);
       
       await BookCacheManager.InvalidateBookCacheAsync(_cacheVersionService,_cacheOptions);
       
       var response = new BookArchive
       {
           Id = id,
           Title = book.Title,
           ArchivedAt = DateTime.UtcNow
       };
       return response;
    }

    /// <inheritdoc />
    public async Task<PaginationResponseBase<Book>> GetBooks(GetBooksRequestModel requestModel)
    {
        var cacheCheckResult = await BookCacheManager.CheckCacheAsync<BookListDto>(
            _cacheVersionService,_cacheService,
            _cacheOptions.BooksListCacheOptions.Prefix, requestModel,
            dto => new Book
            {
                Id = dto.Id,
                Title = dto.Title,
                Authors = dto.Authors,
                Description = dto.Description,
                Year = dto.Year,
                Category = dto.Category,
                Status = dto.Status,
                IsArchived = dto.IsArchived
            });
        if (cacheCheckResult.Count != 0)
        {
            return new PaginationResponseBase<Book>
            {
                Entities = cacheCheckResult
            };
        }
        var booksFromDb = await _bookRepository.GetBooks(requestModel);

        await BookCacheManager.WriteToCacheAsync(
            _cacheVersionService, _cacheService,
            _cacheOptions.BooksListCacheOptions, requestModel, booksFromDb,
            book => new BookListDto
            {
                Id = book.Id,
                Title = book.Title,
                Authors = book.Authors,
                Description = book.Description,
                Year = book.Year,
                Category = book.Category,
                Status = book.Status,
                IsArchived = book.IsArchived
            });
        return new PaginationResponseBase<Book>
        {
            Entities = booksFromDb,
        };
    }
    
    
    public async Task UpdateBookDetails(Guid id, string description, UploadedFile coverFile)
    {
        var book = await _bookRepository.GetBookById(id);

        if (book.IsArchived)
            throw new BookServiceException("Нельзя изменить архивную книгу");
        var currentDate = DateTime.UtcNow;
        var fileName = $"book-covers/{currentDate.Year}/{currentDate.Month}/{id}.";

        if (coverFile?.HasFile == true)
        {
            ValidateCover(coverFile);
            await _minioService.UploadFileAsync(fileName, coverFile.Stream, coverFile.ContentType);
        }

        book.UpdateDetails(description, fileName);

        await _bookRepository.UpdateBook(book, id);

        await BookCacheManager.InvalidateBookCacheAsync(_cacheVersionService,_cacheOptions);
    }

    private void ValidateCover(UploadedFile file)
    {
        if (file.Stream!.Length > 5 * 1024 * 1024)
            throw new BookServiceException("Размер обложки превышает 5 МБ");

        if (file.ContentType is not ("image/jpeg" or "image/png"))
            throw new BookServiceException("Разрешён только JPEG или PNG");
    }
}