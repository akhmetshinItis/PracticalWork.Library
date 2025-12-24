using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using PracticalWork.Library.Abstractions.MessageBroker;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.DTO.BaseDtos;
using PracticalWork.Library.DTO.BookDtos;
using PracticalWork.Library.DTO.LibraryDtos;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Events;
using PracticalWork.Library.Exceptions;
using PracticalWork.Library.Helpers;
using PracticalWork.Library.Models.BookModels;
using PracticalWork.Library.Options;

namespace PracticalWork.Library.Services;

public class LibraryService: ILibraryService
{
    private readonly IReaderRepository _readerRepository;
    private readonly IBookRepository _bookRepository;
    private readonly IBookBorrowRepository _bookBorrowRepository;
    private readonly IMinioService _minioService;
    private readonly ICacheService _cacheService;
    private readonly ICacheVersionService _cacheVersionService;
    private readonly IMessageProducer _producer;
    private readonly BooksCacheOptions _cacheOptions;
    private readonly MinioOptions _minioOptions;
    
    public LibraryService(IReaderRepository readerRepository, 
        IBookRepository bookRepository,
        IBookBorrowRepository bookBorrowRepository,
        ICacheVersionService cacheVersionService,
        IMinioService minioService,
        ICacheService cacheService,
        IMessageProducer producer,
        IOptionsMonitor<BooksCacheOptions> cacheOptions,
        IOptionsMonitor<MinioOptions> minioOptions)
    {
        _readerRepository = readerRepository;
        _bookRepository = bookRepository;
        _bookBorrowRepository = bookBorrowRepository;
        _minioService = minioService;
        _cacheService = cacheService;
        _cacheVersionService = cacheVersionService;
        _cacheOptions = cacheOptions.CurrentValue;
        _minioOptions = minioOptions.CurrentValue;
        _producer = producer;
    }
    
    public async Task BorrowBook(Guid bookId, Guid readerId)
    {
        var book = await _bookRepository.GetBookById(bookId);
        var reader = await _readerRepository.GetReader(readerId);
        if (book.Status is BookStatus.Borrow or BookStatus.Archived)
        {
            throw new LibraryServiceException("Нельзя выдать архивную или выданную книгу");
        }

        if (!reader.IsActive)
        {
            throw new LibraryServiceException("Нельзя выдать книгу с неактивной карточкой");
        }
        var bookBorrow = BookBorrow.CreateBookBorrow();
        book.Status = BookStatus.Borrow;
        await _bookBorrowRepository.CreateBookBorrow(bookId, readerId, bookBorrow);
        await _bookRepository.UpdateBook(book, bookId);
        var message = new BookBorrowedEvent(bookId, readerId, book.Title, 
            reader.FullName, bookBorrow.BorrowDate, bookBorrow.DueDate );
        await _producer.ProduceBookBorrowAsync(message);
        await CacheManager.InvalidateBookCacheAsync(_cacheVersionService,_cacheOptions);
    }

    public async Task ReturnBook(Guid bookId, Guid readerId)
    {
        var (id, bookBorrow) = await _bookBorrowRepository.GetBookBorrow(bookId, readerId);
        var reader = await _readerRepository.GetReader(readerId);
        if (bookBorrow.Status != BookIssueStatus.Issued)
        {
            throw new LibraryServiceException("Книга уже возвращена");
        }
        bookBorrow.ReturnBookBorrow();
        await _bookBorrowRepository.UpdateReturnedBookBorrow(id, bookBorrow);
        var message = new BookReturnedEvent(bookId, readerId, bookBorrow.Book.Title, 
            reader.FullName, bookBorrow.ReturnDate);
        await _producer.ProduceBookReturnAsync(message);
        await CacheManager.InvalidateBookCacheAsync(_cacheVersionService,_cacheOptions);
    }

    public async Task<(Guid bookId, Book book)> GetBookDetails(Guid bookId)
    {
        var cacheCheckResult = await CacheManager.CheckCacheAsync<Book,BookDetailsDto>(
            _cacheVersionService,_cacheService,
            _cacheOptions.BookDetailsCacheOptions.Prefix, bookId,
            dto => new Book
            {
                Title = dto.Title,
                Authors = dto.Authors,
                Category = dto.Category,
                CoverImagePath = dto.CoverImagePath,
                Description = dto.Description,
                Id = dto.Id,
                IsArchived = dto.IsArchived,
                Year = dto.Year,
                Status = dto.Status,
            });
        if (cacheCheckResult.Count != 0)
        {
            var book = cacheCheckResult[0];
            return (book.Id, book);
        }
        var bookFromDb = await _bookRepository.GetBookById(bookId);
        if (bookFromDb.CoverImagePath is not null)
        {
            try
            {
                bookFromDb.CoverImagePath = await _minioService.GetFileUrlAsync(_minioOptions.CoversBucketName, bookFromDb.CoverImagePath);
            }
            catch (Exception)
            {
                throw new LibraryServiceException("Путь к обложке книги невалидный");
            }
        }
        await CacheManager.WriteToCacheAsync(
            _cacheVersionService, _cacheService,
            _cacheOptions.BookDetailsCacheOptions, bookId, [bookFromDb],
            b => new BookDetailsDto
            {
                Id = b.Id,
                Title = b.Title,
                Year = b.Year,
                Description = b.Description,
                Status = b.Status,
                IsArchived = b.IsArchived,
                Authors = b.Authors,
                CoverImagePath = b.CoverImagePath,
                Category = b.Category,
            });
        return (bookId, bookFromDb);
    }

    public async Task<(Guid bookId, Book book)> GetBookDetails(string title)
    {
        var id = await _bookRepository.GetBookIdByTitle(title);
        return await GetBookDetails(id);
    }

    public async Task<PaginationResponseDto<Book>> GetNonArchivedBooksPage(PaginationRequestDto request)
    {
        
        var checkCacheResult = await CacheManager.CheckCacheAsync<Book,LibraryBookDto>(
            _cacheVersionService,
            _cacheService,
            _cacheOptions.LibraryBooksCacheOptions.Prefix,
            request,
            dto => new Book
            {
                Title = dto.Title,
                Authors = dto.Authors,
                Description = dto.Description,
                Year = dto.Year,
                Category = dto.Category,
                Status = dto.Status,
                IsArchived = dto.IsArchived,
                IssuanceRecords = dto.IssuanceRecords.Select(i => new BookBorrow
                    {
                        BorrowDate = i.BorrowDate,
                        ReturnDate = i.ReturnDate,
                        DueDate = i.DueDate,
                        Status = i.Status,
                    }).ToList()
                
            });
        if (checkCacheResult.Count != 0)
        {
            return new PaginationResponseDto<Book>
            {
                Entities = checkCacheResult,
                TotalCount = checkCacheResult.Count,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                PageCount = (int)Math.Ceiling(checkCacheResult.Count / (double)request.PageSize),
            };
        }
        
        var booksFromDb = await _bookRepository
            .GetNonArchivedBooksPageWithIssuanceRecords(request);
        
        await CacheManager.WriteToCacheAsync(_cacheVersionService, _cacheService,
            _cacheOptions.LibraryBooksCacheOptions, request, booksFromDb.Item1,
            book => new LibraryBookDto
            {
                Title = book.Title,
                Authors = book.Authors,
                Description = book.Description,
                Year = book.Year,
                Category = book.Category,
                Status = book.Status,
                IsArchived = book.IsArchived,
                IssuanceRecords = book.IssuanceRecords.Select(i => new IssuanceDto
                {
                    BorrowDate = i.BorrowDate,
                    DueDate = i.DueDate,
                    ReturnDate = i.ReturnDate,
                    Status = i.Status,
                }).ToList()
            });
        return new PaginationResponseDto<Book>
        {
            Entities = booksFromDb.Item1,
            TotalCount = booksFromDb.Item2,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            PageCount = (int)Math.Ceiling(booksFromDb.Item2 / (double)request.PageSize),
        };
    }
}