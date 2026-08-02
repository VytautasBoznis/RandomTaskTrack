using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Request.Base;
using RandomTaskTrack.Data.Response.Base;
using RandomTaskTrack.Interfaces.Base;

namespace RandomTaskTrack.Business.Base;

public abstract class BaseOperation<TRequest, TResponse>
    where TRequest : BaseRequest
    where TResponse : BaseResponse
{
    protected ILogger _logger;
    protected IUnitOfWorkFactory _unitOfWorkFactory;
    protected OperationFactory _operationFactory;

    private readonly IValidator<TRequest> _validator;

    public BaseOperation(ILogger logger, IUnitOfWorkFactory unitOfWorkFactory, OperationFactory operationFactory, IValidator<TRequest> validator)
    {
        _logger = logger;
        _unitOfWorkFactory = unitOfWorkFactory;
        _operationFactory = operationFactory;
        _validator = validator;
    }

    /// <summary>
    /// Opt-in transaction wrapping for write operations. A chat turn can create
    /// half a dozen tasks through tool calls; without this it could commit three
    /// of them and then fail.
    /// </summary>
    protected virtual bool RequiresTransaction => false;

    public async Task<TResponse> Run(TRequest request, IUnitOfWork? unitOfWork = null)
    {
        ValidateRequest(request);

        _logger.LogInformation("Starting operation {OperationName}", GetType().Name);

        bool ownsUnitOfWork = unitOfWork == null;
        IUnitOfWork internalUnitOfWork = unitOfWork ?? await _unitOfWorkFactory.CreateAsync();

        try
        {
            // Only the owner manages the transaction. A nested operation joins
            // the caller's transaction rather than opening a second one.
            if (ownsUnitOfWork && RequiresTransaction)
            {
                await internalUnitOfWork.BeginTransactionAsync();
            }

            TResponse response = await Execute(request, internalUnitOfWork);

            if (ownsUnitOfWork && RequiresTransaction)
            {
                await internalUnitOfWork.CommitAsync();
            }

            _logger.LogInformation("Finished operation {OperationName}", GetType().Name);

            return response;
        }
        catch
        {
            if (ownsUnitOfWork && RequiresTransaction)
            {
                await internalUnitOfWork.RollbackAsync();
            }

            throw;
        }
        finally
        {
            if (ownsUnitOfWork)
            {
                await internalUnitOfWork.DisposeAsync();
            }
        }
    }

    protected abstract Task<TResponse> Execute(TRequest request, IUnitOfWork unitOfWork);

    protected void ValidateRequest(TRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request), "Request cannot be null.");
        }

        ValidationResult result = _validator.Validate(request);

        if (result.IsValid)
        {
            return;
        }

        string description = string.Join("; ", result.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));

        throw new BadRequestException("Invalid request", ExceptionCodes.VALIDATION_FAILED, description);
    }
}
