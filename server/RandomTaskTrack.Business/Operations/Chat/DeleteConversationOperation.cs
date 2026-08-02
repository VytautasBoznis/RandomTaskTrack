using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Request.Chat;
using RandomTaskTrack.Data.Response.Chat;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Chat;

namespace RandomTaskTrack.Business.Operations.Chat;

public class DeleteConversationOperation : BaseOperation<DeleteConversationRequest, DeleteConversationResponse>
{
    private readonly IChatRepository _chatRepository;

    public DeleteConversationOperation(
        ILogger<DeleteConversationOperation> logger,
        IValidator<DeleteConversationRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IChatRepository chatRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _chatRepository = chatRepository;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<DeleteConversationResponse> Execute(DeleteConversationRequest request, IUnitOfWork unitOfWork)
    {
        bool deleted = await _chatRepository.DeleteConversationAsync(request.Id, unitOfWork);

        if (!deleted)
        {
            throw new NotFoundException("No conversation with that id", ExceptionCodes.CONVERSATION_NOT_FOUND);
        }

        return new DeleteConversationResponse { Success = true };
    }
}
