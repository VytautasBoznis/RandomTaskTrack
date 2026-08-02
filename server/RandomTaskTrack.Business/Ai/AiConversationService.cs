using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RandomTaskTrack.Data.Dtos.Chat;
using RandomTaskTrack.Data.Models.Ai;
using RandomTaskTrack.Data.Models.ConfigurationOptions;
using RandomTaskTrack.Data.Models.Chat;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Tasks;
using RandomTaskTrack.Interfaces.Ai;
using RandomTaskTrack.Interfaces.Base;
using RandomTaskTrack.Interfaces.Repositories.Chat;
using RandomTaskTrack.Interfaces.Repositories.Domains;
using RandomTaskTrack.Interfaces.Services;

namespace RandomTaskTrack.Business.Ai;

/// <summary>
/// Owns the agent loop: complete → execute tools → feed results back → repeat
/// until the model stops asking for tools.
///
/// Written by hand rather than using the Anthropic SDK's BetaToolRunner, for
/// two reasons: the runner is Anthropic-specific and would leak through
/// IAiProvider, and owning the loop is what makes it possible to gate
/// destructive tools and persist each turn as it happens.
/// </summary>
public class AiConversationService : IAiConversationService
{
    private static readonly JsonSerializerOptions SerializerOptions = new();

    private readonly IAiProvider _provider;
    private readonly IAiToolRegistry _toolRegistry;
    private readonly IChatRepository _chatRepository;
    private readonly IDomainsRepository _domainsRepository;
    private readonly IClock _clock;
    private readonly AiOptions _options;
    private readonly ILogger<AiConversationService> _logger;

    public AiConversationService(
        IAiProvider provider,
        IAiToolRegistry toolRegistry,
        IChatRepository chatRepository,
        IDomainsRepository domainsRepository,
        IClock clock,
        IOptions<AiOptions> options,
        ILogger<AiConversationService> logger)
    {
        _provider = provider;
        _toolRegistry = toolRegistry;
        _chatRepository = chatRepository;
        _domainsRepository = domainsRepository;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AiTurnResult> RunTurnAsync(Guid conversationId, int? domainId, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        List<TaskDomain> domains = await _domainsRepository.GetAllAsync(false, unitOfWork);
        TaskDomain? focusDomain = domainId.HasValue ? domains.FirstOrDefault(d => d.Id == domainId.Value) : null;

        var request = new AiRequest
        {
            SystemPrompt = AiSystemPrompt.Build(_clock.Today, _clock.TimeZone.Id, domains, focusDomain),
            Tools = _toolRegistry.GetDefinitions(),
            MaxTokens = _options.MaxTokens,
            Messages = await LoadHistoryAsync(conversationId, unitOfWork)
        };

        var result = new AiTurnResult();
        int seq = await _chatRepository.GetNextSeqAsync(conversationId, unitOfWork);

        for (int iteration = 0; iteration < _options.MaxToolIterations; iteration++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AiResponse response = await _provider.CompleteAsync(request, cancellationToken);

            result.InputTokens += response.Usage.InputTokens;
            result.OutputTokens += response.Usage.OutputTokens;
            result.Model = response.Model;

            if (response.StopReason == AiStopReason.Refusal)
            {
                throw new AiProviderException(
                    "The AI declined to answer that request.",
                    ExceptionCodes.AI_PROVIDER_FAILED);
            }

            AiMessage assistantMessage = AiMessage.FromAssistant(response.Content, response.ToolCalls);
            request.Messages.Add(assistantMessage);

            await _chatRepository.AddMessageAsync(new ChatMessage
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                Seq = seq++,
                Role = "assistant",
                Content = response.Content,
                ToolCalls = response.ToolCalls.Count > 0 ? SerializeToolCalls(response.ToolCalls) : null,
                Model = response.Model,
                InputTokens = response.Usage.InputTokens,
                OutputTokens = response.Usage.OutputTokens
            }, unitOfWork);

            if (response.ToolCalls.Count == 0)
            {
                result.Reply = response.Content ?? string.Empty;
                return result;
            }

            var toolResults = new List<AiToolResult>();

            foreach (AiToolCall call in response.ToolCalls)
            {
                AiToolResult toolResult = await _toolRegistry.ExecuteAsync(call, unitOfWork, cancellationToken);
                toolResults.Add(toolResult);

                result.AppliedToolCalls.Add(new AppliedToolCallDto
                {
                    Name = call.Name,
                    Input = call.JsonInput,
                    Result = toolResult.Content,
                    IsError = toolResult.IsError
                });
            }

            // All results go back in one turn — splitting them trains the model
            // out of making parallel tool calls.
            request.Messages.Add(AiMessage.FromToolResults(toolResults));

            await _chatRepository.AddMessageAsync(new ChatMessage
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                Seq = seq++,
                Role = "tool",
                ToolResults = SerializeToolResults(toolResults)
            }, unitOfWork);
        }

        // The loop is bounded so a model that keeps calling tools cannot rack up
        // unbounded billed round trips. Surfaced rather than silently truncated.
        _logger.LogWarning("Conversation {ConversationId} hit the {Max}-iteration tool limit", conversationId, _options.MaxToolIterations);

        throw new AiProviderException(
            $"The assistant used more than {_options.MaxToolIterations} tool steps without finishing.",
            ExceptionCodes.AI_TOOL_LIMIT_EXCEEDED,
            "Try breaking the request into smaller steps.");
    }

    public async Task<string> GenerateTitleAsync(string firstMessage, CancellationToken cancellationToken)
    {
        var request = new AiRequest
        {
            SystemPrompt = "Write a short title (max 6 words) for a conversation that starts with the message below. Reply with the title only — no quotes, no punctuation at the end.",
            MaxTokens = 64,
            Messages = [AiMessage.FromUser(firstMessage)]
        };

        AiResponse response = await _provider.CompleteAsync(request, cancellationToken);

        return (response.Content ?? string.Empty).Trim().Trim('"');
    }

    private async Task<List<AiMessage>> LoadHistoryAsync(Guid conversationId, IUnitOfWork unitOfWork)
    {
        List<ChatMessage> stored = await _chatRepository.GetMessagesAsync(conversationId, unitOfWork);
        var messages = new List<AiMessage>();

        foreach (ChatMessage message in stored)
        {
            switch (message.Role)
            {
                case "user":
                    messages.Add(AiMessage.FromUser(message.Content ?? string.Empty));
                    break;

                case "assistant":
                    messages.Add(AiMessage.FromAssistant(message.Content, DeserializeToolCalls(message.ToolCalls)));
                    break;

                case "tool":
                    List<AiToolResult> results = DeserializeToolResults(message.ToolResults);

                    // A tool turn with no results would leave the preceding
                    // tool_use blocks unanswered, which the API rejects.
                    if (results.Count > 0)
                    {
                        messages.Add(AiMessage.FromToolResults(results));
                    }

                    break;
            }
        }

        return messages;
    }

    private static string SerializeToolCalls(List<AiToolCall> calls) => JsonSerializer.Serialize(calls, SerializerOptions);

    private static string SerializeToolResults(List<AiToolResult> results) => JsonSerializer.Serialize(results, SerializerOptions);

    private static List<AiToolCall> DeserializeToolCalls(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<AiToolCall>>(json, SerializerOptions) ?? [];

    private static List<AiToolResult> DeserializeToolResults(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<AiToolResult>>(json, SerializerOptions) ?? [];
}
