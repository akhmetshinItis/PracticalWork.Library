using PracticalWork.Library.Abstractions.MessageBroker;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Events;
using PracticalWork.Library.Exceptions;
using PracticalWork.Library.Models.BookModels;
using PracticalWork.Library.Models.ReaderModels;

namespace PracticalWork.Library.Services;

public class ReaderService: IReaderService
{
    private readonly IReaderRepository _readerRepository;
    private readonly IMessageProducer _producer;
    public ReaderService(IReaderRepository repository,
        IMessageProducer producer)
    {
        _readerRepository = repository;
        _producer = producer;
    }
    public async Task<Guid> CreateReader(Reader reader)
    {
        if (await _readerRepository.IsExistReader(reader.PhoneNumber))
        {
            throw new ReaderServiceException("Phone number is not unique");
        }
        reader.IsActive = true;
        var id = await _readerRepository.CreateReader(reader);
        var message = new ReaderCreatedEvent(id, reader.FullName,
            reader.PhoneNumber, reader.ExpiryDate, DateTime.UtcNow);
        await _producer.ProduceReaderCreateAsync(message);
        return id;
    }

    public async Task ExtendExpiryDate(Guid id, DateOnly date)
    {
        var reader = await _readerRepository.GetReader(id);
        if (!reader.IsActive)
        {
            throw new ReaderServiceException("Карточка неактивна");
        }

        if (reader.ExpiryDate >= date)
        {
            throw new ReaderServiceException("Необходимо продлить карточку на будущую дату");
        }
        reader.ExpiryDate = date;
        await _readerRepository.UpdateReader(id, reader);
    }

    public async Task<(bool borrowBooksExist, IReadOnlyList<Book> borrowBooks)> CloseReader(Guid id)
    {
        var readerWithBorrowBooks = await _readerRepository.GetReaderWithBorrowBooks(id);
        var borrowBooksExist = readerWithBorrowBooks.BorrowBooks.Any();
        if (borrowBooksExist)
        {
            return (true, readerWithBorrowBooks.BorrowBooks);
        }
        readerWithBorrowBooks.IsActive = false;
        readerWithBorrowBooks.ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow);
        await _readerRepository.UpdateReader(id, readerWithBorrowBooks);
        var message = new ReaderClosedEvent(id, readerWithBorrowBooks.FullName,
            DateTime.UtcNow, "Вызван метод закрытия карточки");
        await _producer.ProduceReaderCloseAsync(message);
        return (false, readerWithBorrowBooks.BorrowBooks);
    }

    public async Task<IReadOnlyList<BorrowedBook>> GetAllBorrowBooks(Guid readerId)
    {
        var result = await _readerRepository
            .GetReadersBorrowBooks(readerId);
        return result;
    }
}