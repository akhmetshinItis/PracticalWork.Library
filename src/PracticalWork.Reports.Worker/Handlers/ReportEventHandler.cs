using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PracticalWork.Library.Abstractions.MessageBroker;
using PracticalWork.Library.Events;
using PracticalWork.Reports.Worker.Abstractions;

namespace PracticalWork.Reports.Worker.Handlers;

public sealed class ReportEventHandler : IMessageHandler<ReportCreateEvent>
{
    private readonly IReportGenerationService _reportGenerationService;

    public ReportEventHandler(IReportGenerationService reportGenerationService)
    {
        _reportGenerationService = reportGenerationService;
    }

    public async Task HandleAsync(ReportCreateEvent reportEvent, CancellationToken cancellationToken)
    {
        await _reportGenerationService.GenerateReportAsync(
            reportEvent.Id,
            reportEvent.PeriodFrom,
            reportEvent.PeriodTo,
            reportEvent.EventTypes.ToArray(),
            cancellationToken);
    }
}
