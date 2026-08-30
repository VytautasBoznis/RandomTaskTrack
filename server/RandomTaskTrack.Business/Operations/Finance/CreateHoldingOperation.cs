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

public class CreateHoldingOperation : BaseOperation<CreateHoldingRequest, CreateHoldingResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public CreateHoldingOperation(
        ILogger<CreateHoldingOperation> logger,
        IValidator<CreateHoldingRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<CreateHoldingResponse> Execute(CreateHoldingRequest request, IUnitOfWork unitOfWork)
    {
        string symbol = request.Symbol.Trim();

        FinanceAccount account = await FinanceGuards.ResolveAccountAsync(request.AccountId, _financeRepository, unitOfWork);

        // Per account, not globally: the same ETF in a brokerage and a pension
        // is two holdings. ux_fin_holdings_account_symbol would catch a repeat
        // within one account, but as a constraint name rather than a sentence —
        // and adding a symbol you already hold there is an ordinary mistake.
        if (await _financeRepository.GetHoldingBySymbolAsync(account.Id, symbol, unitOfWork) is not null)
        {
            throw new BadRequestException(
                $"{account.Name} already holds {symbol}.",
                ExceptionCodes.FINANCE_SYMBOL_EXISTS,
                "Add a trade to the existing holding instead.");
        }

        var holding = new Holding
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Symbol = symbol,
            Name = request.Name,
            Currency = await FinanceGuards.ResolveCurrencyAsync(request.Currency, _financeRepository, unitOfWork)
        };

        await _financeRepository.CreateHoldingAsync(holding, unitOfWork);

        return new CreateHoldingResponse
        {
            Holding = await _financeRepository.GetHoldingAsync(holding.Id, unitOfWork)
                      ?? throw new NotFoundException("Holding not found after create", ExceptionCodes.FINANCE_HOLDING_NOT_FOUND)
        };
    }
}
