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

public class CreateTradeOperation : BaseOperation<CreateTradeRequest, CreateTradeResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public CreateTradeOperation(
        ILogger<CreateTradeOperation> logger,
        IValidator<CreateTradeRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<CreateTradeResponse> Execute(CreateTradeRequest request, IUnitOfWork unitOfWork)
    {
        if (await _financeRepository.GetHoldingAsync(request.HoldingId, unitOfWork) is null)
        {
            throw new NotFoundException("Holding not found", ExceptionCodes.FINANCE_HOLDING_NOT_FOUND);
        }

        var trade = new Trade
        {
            Id = Guid.NewGuid(),
            HoldingId = request.HoldingId,
            Side = request.Side,
            Quantity = request.Quantity,
            Price = request.Price,
            Fee = request.Fee ?? 0m,
            TradedOn = request.TradedOn,
            Note = request.Note
        };

        await _financeRepository.CreateTradeAsync(trade, unitOfWork);

        return new CreateTradeResponse
        {
            Trade = await _financeRepository.GetTradeAsync(trade.Id, unitOfWork)
                    ?? throw new NotFoundException("Trade not found after create", ExceptionCodes.FINANCE_TRADE_NOT_FOUND)
        };
    }
}
