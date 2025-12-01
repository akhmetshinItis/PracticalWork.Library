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

    /// <summary>
    /// Конструктор
    /// </summary>
    /// <param name="bookRepository">Репозиторий работы с книгами</param>
    /// <param name="cacheService">Сервис кеша</param>
    /// <param name="cacheVersionService">Сервис версионирования кеша</param>
    /// <param name="cacheOptions">Опции кеша для книг</param>
    public BookService(
        IBookRepository bookRepository,
        ICacheService cacheService,
        ICacheVersionService cacheVersionService,
        IOptions<BooksCacheOptions> cacheOptions)
    {
        _bookRepository = bookRepository;
        _cacheService = cacheService;
        _cacheVersionService = cacheVersionService;
        _cacheOptions = cacheOptions.Value;
    }
    
    /// <inheritdoc/> 
    public async Task<Guid> CreateBook(Book book)
    {
        book.Status = BookStatus.Available;
        try
        {
            var bookId = await _bookRepository.CreateBook(book);
            
            if (_cacheOptions?.BooksListCacheOptions?.Prefix != null)
            {
                await _cacheVersionService.IncrementVersionAsync(_cacheOptions.BooksListCacheOptions.Prefix);
            }
            
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
        
        await InvalidateBookCacheAsync(id);
    }

    /// <inheritdoc/> 
    public async Task ArchiveBook(Guid id)
    {
       var book = await _bookRepository.GetBookById(id);
       book.Archive();
       
       await _bookRepository.UpdateBook(book, id);
       
       await InvalidateBookCacheAsync(id);
    }

    /// <inheritdoc />
    public async Task<PaginationResponseBase<Book>> GetBooks(GetBooksRequestModel requestModel)
    {
        if (_cacheOptions?.BooksListCacheOptions?.Prefix != null)
        {
            var version = await _cacheVersionService.GetVersionAsync(_cacheOptions.BooksListCacheOptions.Prefix);
            var cacheKey = CacheKeyHasher.GenerateCacheKey(
                _cacheOptions.BooksListCacheOptions.Prefix,
                version,
                new
                {
                    requestModel
                });

            var cachedBooks = await _cacheService.GetAsync<List<BookListDto>>(cacheKey);
            if (cachedBooks != null && cachedBooks.Count != 0)
            {
                var books = cachedBooks.Select(dto => new Book
                {
                    Id = dto.Id,
                    Title = dto.Title,
                    Authors = dto.Authors,
                    Description = dto.Description,
                    Year = dto.Year,
                    Category = dto.Category,
                    Status = dto.Status,
                    CoverImagePath = dto.CoverImagePath,
                    IsArchived = dto.IsArchived
                }).ToList();

                return new PaginationResponseBase<Book>
                {
                    Entities = books,
                };
            }
        }

        var booksFromDb = await _bookRepository.GetBooks(requestModel);

        if (_cacheOptions?.BooksListCacheOptions != null)
        {
            var version = await _cacheVersionService.GetVersionAsync(_cacheOptions.BooksListCacheOptions.Prefix);
            var cacheKey = CacheKeyHasher.GenerateCacheKey(
                _cacheOptions.BooksListCacheOptions.Prefix,
                version,
                new
                {
                    requestModel
                });

            var booksListDto = booksFromDb.Select(book => new BookListDto
            {
                Id = book.Id,
                Title = book.Title,
                Authors = book.Authors,
                Description = book.Description,
                Year = book.Year,
                Category = book.Category,
                Status = book.Status,
                CoverImagePath = book.CoverImagePath,
                IsArchived = book.IsArchived
            }).ToList();

            var ttl = _cacheOptions.BooksListCacheOptions.TtlMinutes;
            await _cacheService.SetAsync(cacheKey, booksListDto, TimeSpan.FromMinutes(ttl));
        }

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

        string coverPath = book.CoverImagePath;

        if (coverFile?.HasFile == true)
        {
            ValidateCover(coverFile);

            // coverPath = await _fileStorage.UploadBookCover(id, coverFile);
        }

        book.UpdateDetails(description, coverPath);

        await _bookRepository.UpdateBook(book, id);

        await InvalidateBookCacheAsync(id);
    }

    private void ValidateCover(UploadedFile file)
    {
        if (file.Stream!.Length > 5 * 1024 * 1024)
            throw new BookServiceException("Размер обложки превышает 5 МБ");

        if (file.ContentType is not ("image/jpeg" or "image/png"))
            throw new BookServiceException("Разрешён только JPEG или PNG");
    }

    /// <summary>
    /// Инвалидация кеша связанных данных при изменении книги.
    /// Инкрементирует версию кеша по префиксу, что делает все старые ключи невалидными.
    /// Ключи генерируются через CacheKeyHasher в формате: {prefix}:v{version}:{hash}
    /// </summary>
    /// <param name="bookId">Идентификатор книги</param>
    private async Task InvalidateBookCacheAsync(Guid bookId)
    {
        // Инвалидация кеша списка книг (books:list:{hash})
        if (_cacheOptions?.BooksListCacheOptions?.Prefix != null)
        {
            await _cacheVersionService.IncrementVersionAsync(_cacheOptions.BooksListCacheOptions.Prefix);
        }

        // Инвалидация кеша списка книг для модуля работы библиотеки (library:books:{hash})
        if (_cacheOptions?.LibraryBooksCacheOptions?.Prefix != null)
        {
            await _cacheVersionService.IncrementVersionAsync(_cacheOptions.LibraryBooksCacheOptions.Prefix);
        }
    }

}