using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Dtos.Chat;
using RandomTaskTrack.Data.Models.Chat;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Request.Chat;
using RandomTaskTrack.Data.Response.Chat;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Chat;

namespace RandomTaskTrack.Business.Operations.Chat;

public class GetConversationOperation : BaseOperation<GetConversationRequest, GetConversationResponse>
{
    private readonly IChatRepository _chatRepository;

    public GetConversationOperation(
        ILogger<GetConversationOperation> logger,
        IValidator<GetConversationRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IChatRepository chatRepository) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _chatRepository = chatRepository;
    }

    protected override async Task<GetConversationResponse> Execute(GetConversationRequest request, IUnitOfWork unitOfWork)
    {
        ChatConversation conversation = await _chatRepository.GetConversationAsync(request.Id, unitOfWork)
                                        ?? throw new NotFoundException("No conversation with that id", ExceptionCodes.CONVERSATION_NOT_FOUND);

        List<ChatMessage> messages = await _chatRepository.GetMessagesAsync(conversation.Id, unitOfWork);

        return new GetConversationResponse
        {
            Conversation = new ConversationDetailDto
            {
                Id = conversation.Id,
                Title = conversation.Title,
                DomainId = conversation.DomainId,

                // Tool turns are the model's own bookkeeping — they carry no
                // user-readable content, so they stay out of the transcript.
                Messages = messages
                    .Where(m => m.Role != "tool")
                    .Select(m => new ChatMessageDto
                    {
                        Id = m.Id,
                        Seq = m.Seq,
                        Role = m.Role,
                        Content = m.Content,
                        ToolCalls = m.ToolCalls,
                        CreatedAt = m.CreatedAt
                    })
                    .ToList()
            }
        };
    }
}
