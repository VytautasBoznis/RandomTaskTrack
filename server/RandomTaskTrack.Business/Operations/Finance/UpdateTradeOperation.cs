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

/// <summary>
/// The manual-correction path. Positions are summed from trades and never
/// stored, so fixing a mistyped quantity here fixes the position, the market
/// value and the net worth in the same breath.
/// </summary>
public class UpdateTradeOperation : BaseOperation<UpdateTradeRequest, UpdateTradeResponse>
{
    private readonly IFinanceRepository _financeRepository;

    public UpdateTradeOperation(
        ILogger<UpdateTradeOperation> logger,
        IValidator<UpdateTradeRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IFinanceRepository financeRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _financeRepository = financeRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<UpdateTradeResponse> Execute(UpdateTradeRequest request, IUnitOfWork unitOfWork)
    {
        Trade trade = await _financeRepository.GetTradeAsync(request.Id, unitOfWork)
                      ?? throw new NotFoundException("Trade not found", ExceptionCodes.FINANCE_TRADE_NOT_FOUND);

        trade.Side = request.Side ?? trade.Side;
        trade.Quantity = request.Quantity ?? trade.Quantity;
        trade.Price = request.Price ?? trade.Price;
        trade.Fee = request.Fee ?? trade.Fee;
        trade.TradedOn = request.TradedOn ?? trade.TradedOn;
        trade.Note = request.Note ?? trade.Note;

        await _financeRepository.UpdateTradeAsync(trade, unitOfWork);

        return new UpdateTradeResponse
        {
            Trade = await _financeRepository.GetTradeAsync(trade.Id, unitOfWork)
                    ?? throw new NotFoundException("Trade not found after update", ExceptionCodes.FINANCE_TRADE_NOT_FOUND)
        };
    }
}
