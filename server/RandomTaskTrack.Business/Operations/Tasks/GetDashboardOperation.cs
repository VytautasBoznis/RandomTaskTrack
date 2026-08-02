using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Dtos.Tasks;
using RandomTaskTrack.Data.Request.Tasks;
using RandomTaskTrack.Data.Response.Tasks;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Tasks;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Operations.Tasks;

public class GetDashboardOperation : BaseOperation<GetDashboardRequest, GetDashboardResponse>
{
    private readonly ITasksRepository _tasksRepository;
    private readonly IClock _clock;

    public GetDashboardOperation(
        ILogger<GetDashboardOperation> logger,
        IValidator<GetDashboardRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        ITasksRepository tasksRepository,
        IClock clock) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _tasksRepository = tasksRepository;
        _clock = clock;
    }

    protected override async Task<GetDashboardResponse> Execute(GetDashboardRequest request, IUnitOfWork unitOfWork)
    {
        DateOnly today = _clock.Today;

        var dashboard = new DashboardDto
        {
            Today = today,
            Overdue = await _tasksRepository.GetOverdueAsync(today, unitOfWork),
            DueToday = await _tasksRepository.GetDueOnAsync(today, unitOfWork),
            Upcoming = await _tasksRepository.GetUpcomingAsync(today, today.AddDays(request.UpcomingDays), unitOfWork),
            CompletedToday = await _tasksRepository.GetCompletedOnAsync(today, unitOfWork),
            Streaks = await _tasksRepository.GetDomainStreaksAsync(today, unitOfWork)
        };

        return new GetDashboardResponse { Dashboard = dashboard };
    }
}
