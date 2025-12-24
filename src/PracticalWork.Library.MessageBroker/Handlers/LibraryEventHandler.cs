using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PracticalWork.Library.Abstractions.MessageBroker;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Events;
using PracticalWork.Library.Models.ReportModels;

namespace PracticalWork.Library.MessageBroker.Handlers
{
    public class LibraryEventHandler : IMessageHandler<BaseLibraryEvent>
    {
        private readonly IServiceProvider _serviceProvider;

        public LibraryEventHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task HandleAsync(BaseLibraryEvent libraryEvent, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();

            var log = new ActivityLog
            {
                Event = libraryEvent,
                EventDate = libraryEvent.OccurredOn,
                EventType = libraryEvent.EventType
            };

            await reportService.WriteSystemActivityLogs(log);
        }
    }

}