using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Request.Recipes;
using RandomTaskTrack.Data.Response.Recipes;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Recipes;

namespace RandomTaskTrack.Business.Operations.Recipes;

public class GetCatalogStatusOperation : BaseOperation<GetCatalogStatusRequest, GetCatalogStatusResponse>
{
    private readonly IRecipeCatalogImporter _importer;

    public GetCatalogStatusOperation(
        ILogger<GetCatalogStatusOperation> logger,
        IValidator<GetCatalogStatusRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IRecipeCatalogImporter importer) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _importer = importer;
    }

    protected override async Task<GetCatalogStatusResponse> Execute(GetCatalogStatusRequest request, IUnitOfWork unitOfWork)
    {
        return new GetCatalogStatusResponse
        {
            Status = await _importer.GetStatusAsync(CancellationToken.None)
        };
    }
}
