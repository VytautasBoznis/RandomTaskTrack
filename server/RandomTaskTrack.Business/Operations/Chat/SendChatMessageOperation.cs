using FluentValidation;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Models.Chat;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Request.Chat;
using RandomTaskTrack.Data.Response.Chat;
using RandomTaskTrack.Interfaces.Ai;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Chat;

namespace RandomTaskTrack.Business.Operations.Chat;

/// <summary>
/// One chat turn. Runs in a transaction so that a turn which creates six tasks
/// through tool calls either lands completely or not at all — a half-applied
/// plan is worse than a failed one.
/// </summary>
public class SendChatMessageOperation : BaseOperation<SendChatMessageRequest, SendChatMessageResponse>
{
    private const int TitleFallbackLength = 60;

    private readonly IChatRepository _chatRepository;
    private readonly IAiConversationService _conversationService;

    public SendChatMessageOperation(
        ILogger<SendChatMessageOperation> logger,
        IValidator<SendChatMessageRequest> validator,
        IUnitOfWorkFactory unitOfWorkFactory,
        OperationFactory operationFactory,
        IChatRepository chatRepository,
        IAiConversationService conversationService) : base(logger, unitOfWorkFactory, operationFactory, validator)
    {
        _chatRepository = chatRepository;
        _conversationService = conversationService;
    }

    protected override bool RequiresTransaction => true;

    protected override async Task<SendChatMessageResponse> Execute(SendChatMessageRequest request, IUnitOfWork unitOfWork)
    {
        ChatConversation conversation = await ResolveConversationAsync(request, unitOfWork);

        await _chatRepository.AddMessageAsync(new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Seq = await _chatRepository.GetNextSeqAsync(conversation.Id, unitOfWork),
            Role = "user",
            Content = request.Message
        }, unitOfWork);

        AiTurnResult result = await _conversationService.RunTurnAsync(
            conversation.Id,
            request.DomainId ?? conversation.DomainId,
            unitOfWork,
            CancellationToken.None);

        await _chatRepository.TouchConversationAsync(conversation.Id, unitOfWork);

        return new SendChatMessageResponse
        {
            ConversationId = conversation.Id,
            Reply = result.Reply,
            AppliedToolCalls = result.AppliedToolCalls,
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens
        };
    }

    private async Task<ChatConversation> ResolveConversationAsync(SendChatMessageRequest request, IUnitOfWork unitOfWork)
    {
        if (request.ConversationId.HasValue)
        {
            return await _chatRepository.GetConversationAsync(request.ConversationId.Value, unitOfWork)
                   ?? throw new NotFoundException("No conversation with that id", ExceptionCodes.CONVERSATION_NOT_FOUND);
        }

        var conversation = new ChatConversation
        {
            Id = Guid.NewGuid(),
            Title = await GenerateTitleAsync(request.Message),
            DomainId = request.DomainId
        };

        return await _chatRepository.CreateConversationAsync(conversation, unitOfWork);
    }

    private async Task<string> GenerateTitleAsync(string firstMessage)
    {
        try
        {
            string title = await _conversationService.GenerateTitleAsync(firstMessage, CancellationToken.None);

            if (!string.IsNullOrWhiteSpace(title))
            {
                return title;
            }
        }
        catch (Exception ex)
        {
            // A cosmetic title is never worth failing the user's actual message.
            _logger.LogWarning(ex, "Conversation title generation failed; falling back to the message text");
        }

        return firstMessage.Length <= TitleFallbackLength
            ? firstMessage
            : firstMessage[..TitleFallbackLength] + "…";
    }
}
