using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Request.Recipes;
using RandomTaskTrack.Data.Response.Recipes;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Recipes;

namespace RandomTaskTrack.Business.Operations.Recipes;

/// <summary>
/// Returns as soon as the run is queued — the import takes minutes and the tab
/// polls the status endpoint rather than holding a request open.
/// </summary>
public class StartCatalogImportOperation : BaseOperation<StartCatalogImportRequest, StartCatalogImportResponse>
{
    private readonly IRecipeCatalogImporter _importer;

    public StartCatalogImportOperation(
        ILogger<StartCatalogImportOperation> logger,
        IValidator<StartCatalogImportRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IRecipeCatalogImporter importer) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _importer = importer;
    }

    protected override async Task<StartCatalogImportResponse> Execute(StartCatalogImportRequest request, IUnitOfWork unitOfWork)
    {
        bool started = _importer.TryStart();

        return new StartCatalogImportResponse
        {
            Started = started,
            Status = await _importer.GetStatusAsync(CancellationToken.None)
        };
    }
}
