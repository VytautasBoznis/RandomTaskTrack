using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Request.Domains;
using RandomTaskTrack.Data.Response.Domains;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Domains;

namespace RandomTaskTrack.Business.Operations.Domains;

public class GetDomainsOperation : BaseOperation<GetDomainsRequest, GetDomainsResponse>
{
    private readonly IDomainsRepository _domainsRepository;

    public GetDomainsOperation(
        ILogger<GetDomainsOperation> logger,
        IValidator<GetDomainsRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IDomainsRepository domainsRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _domainsRepository = domainsRepository;
    }

    protected override async Task<GetDomainsResponse> Execute(GetDomainsRequest request, IUnitOfWork unitOfWork)
    {
        return new GetDomainsResponse
        {
            Domains = await _domainsRepository.GetAllAsync(request.IncludeInactive, unitOfWork)
        };
    }
}
