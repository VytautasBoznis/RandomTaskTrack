using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Request.Recurrences;
using RandomTaskTrack.Data.Response.Recurrences;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Recurrences;

namespace RandomTaskTrack.Business.Operations.Recurrences;

public class GetRecurrencesOperation : BaseOperation<GetRecurrencesRequest, GetRecurrencesResponse>
{
    private readonly IRecurrencesRepository _recurrencesRepository;

    public GetRecurrencesOperation(
        ILogger<GetRecurrencesOperation> logger,
        IValidator<GetRecurrencesRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IRecurrencesRepository recurrencesRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _recurrencesRepository = recurrencesRepository;
    }

    protected override async Task<GetRecurrencesResponse> Execute(GetRecurrencesRequest request, IUnitOfWork unitOfWork)
    {
        return new GetRecurrencesResponse
        {
            Recurrences = await _recurrencesRepository.QueryAsync(request.DomainId, request.IncludeInactive, unitOfWork)
        };
    }
}
