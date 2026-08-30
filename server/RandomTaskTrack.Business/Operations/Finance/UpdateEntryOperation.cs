using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Finance;
using RandomTaskTrack.Data.Request.Finance;
using RandomTaskTrack.Data.Response.Finance;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Finance;

namespace RandomTaskTrack.Business.Operations.Finance;

public class UpdateEntryOperation : BaseOperation<UpdateEntryRequest, UpdateEntryResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public UpdateEntryOperation(
        ILogger<UpdateEntryOperation> logger,
        IValidator<UpdateEntryRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<UpdateEntryResponse> Execute(UpdateEntryRequest request, IUnitOfWork unitOfWork)
    {
        LedgerEntry entry = await _financeRepository.GetEntryAsync(request.Id, unitOfWork)
                            ?? throw new NotFoundException("Entry not found", ExceptionCodes.FINANCE_ENTRY_NOT_FOUND);

        entry.Name = request.Name ?? entry.Name;
        entry.Amount = request.Amount ?? entry.Amount;
        entry.OccurredOn = request.OccurredOn ?? entry.OccurredOn;
        entry.Category = request.Category ?? entry.Category;
        entry.Note = request.Note ?? entry.Note;

        if (request.AccountId.HasValue)
        {
            entry.AccountId = (await FinanceGuards.ResolveAccountAsync(request.AccountId.Value, _financeRepository, unitOfWork)).Id;
        }

        if (request.Currency is not null)
        {
            entry.Currency = await FinanceGuards.ResolveCurrencyAsync(request.Currency, _financeRepository, unitOfWork);
        }

        await _financeRepository.UpdateEntryAsync(entry, unitOfWork);

        return new UpdateEntryResponse
        {
            Entry = await _financeRepository.GetEntryAsync(entry.Id, unitOfWork)
                    ?? throw new NotFoundException("Entry not found after update", ExceptionCodes.FINANCE_ENTRY_NOT_FOUND)
        };
    }
}
