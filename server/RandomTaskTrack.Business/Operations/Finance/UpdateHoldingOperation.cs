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

public class UpdateHoldingOperation : BaseOperation<UpdateHoldingRequest, UpdateHoldingResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public UpdateHoldingOperation(
        ILogger<UpdateHoldingOperation> logger,
        IValidator<UpdateHoldingRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<UpdateHoldingResponse> Execute(UpdateHoldingRequest request, IUnitOfWork unitOfWork)
    {
        Holding holding = await _financeRepository.GetHoldingAsync(request.Id, unitOfWork)
                          ?? throw new NotFoundException("Holding not found", ExceptionCodes.FINANCE_HOLDING_NOT_FOUND);

        holding.Name = request.Name ?? holding.Name;

        if (request.Symbol is not null)
        {
            string symbol = request.Symbol.Trim();
            Holding? clash = await _financeRepository.GetHoldingBySymbolAsync(symbol, unitOfWork);

            if (clash is not null && clash.Id != holding.Id)
            {
                throw new BadRequestException(
                    $"You already hold {symbol}.",
                    ExceptionCodes.FINANCE_SYMBOL_EXISTS);
            }

            holding.Symbol = symbol;
        }

        if (request.Currency is not null)
        {
            holding.Currency = await FinanceGuards.ResolveCurrencyAsync(request.Currency, _financeRepository, unitOfWork);
        }

        await _financeRepository.UpdateHoldingAsync(holding, unitOfWork);

        return new UpdateHoldingResponse
        {
            Holding = await _financeRepository.GetHoldingAsync(holding.Id, unitOfWork)
                      ?? throw new NotFoundException("Holding not found after update", ExceptionCodes.FINANCE_HOLDING_NOT_FOUND)
        };
    }
}
