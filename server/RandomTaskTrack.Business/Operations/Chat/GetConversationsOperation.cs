using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Request.Chat;
using RandomTaskTrack.Data.Response.Chat;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Chat;

namespace RandomTaskTrack.Business.Operations.Chat;

public class GetConversationsOperation : BaseOperation<GetConversationsRequest, GetConversationsResponse>
{
    private readonly IChatRepository _chatRepository;

    public GetConversationsOperation(
        ILogger<GetConversationsOperation> logger,
        IValidator<GetConversationsRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IChatRepository chatRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _chatRepository = chatRepository;
    }

    protected override async Task<GetConversationsResponse> Execute(GetConversationsRequest request, IUnitOfWork unitOfWork)
    {
        return new GetConversationsResponse
        {
            Conversations = await _chatRepository.GetConversationsAsync(request.Limit, unitOfWork)
        };
    }
}
