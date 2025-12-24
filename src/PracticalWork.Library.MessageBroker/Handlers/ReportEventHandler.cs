using Microsoft.Extensions.DependencyInjection;
using PracticalWork.Library.Abstractions.MessageBroker;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Events;

namespace PracticalWork.Library.MessageBroker.Handlers
{
    public class ReportEventHandler : IMessageHandler<ReportCreateEvent>
    {
        private readonly IServiceProvider _serviceProvider;

        public ReportEventHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task HandleAsync(ReportCreateEvent reportEvent, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();

            await reportService.GenerateReport(
                reportEvent.Id,
                reportEvent.PeriodFrom,
                reportEvent.PeriodTo,
                reportEvent.EventTypes.ToArray());
        }
    }

}