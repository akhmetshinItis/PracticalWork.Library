using PracticalWork.Library.Abstractions.MessageBroker;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Events;
using PracticalWork.Library.Models.ReportModels;

namespace PracticalWork.Library.MessageBroker.Handlers
{
    /// <inheritdoc />
    public class LibraryEventHandler : IMessageHandler<BaseLibraryEvent>
    {
        private readonly IActivityLogRepository _activityLogRepository;

        public LibraryEventHandler(IActivityLogRepository activityLogRepository)
        {
            _activityLogRepository = activityLogRepository;
        }

        public async Task HandleAsync(BaseLibraryEvent libraryEvent, CancellationToken cancellationToken)
        {
            var log = new ActivityLog
            {
                Event = libraryEvent,
                EventDate = libraryEvent.OccurredOn,
                EventType = libraryEvent.EventType
            };

            await _activityLogRepository.AddLogAsync(log);
        }
    }

}