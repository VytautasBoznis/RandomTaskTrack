using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RandomTaskTrack.Data.Models.ConfigurationOptions;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Interfaces.Ai;
using AiModels = RandomTaskTrack.Data.Models.Ai;
using AppEnums = RandomTaskTrack.Data.Models.Enums;

namespace RandomTaskTrack.Business.Ai.Providers;

/// <summary>
/// Translates the provider-neutral AiRequest/AiResponse into Anthropic's
/// Messages API and back. Everything Anthropic-specific — effort, thinking,
/// prompt caching — is confined to this file; nothing above it knows which
/// provider is in use.
/// </summary>
public class AnthropicAiProvider : IAiProvider
{
    /// <summary>
    /// How many times a single CompleteAsync will resume a turn the server
    /// paused mid-search. Bounded for the same reason the agent loop is: a
    /// model that never stops searching must not run up an unbounded bill.
    /// </summary>
    private const int MaxPauseResumes = 4;

    /// <summary>
    /// Searches are billed per use on top of tokens, so the model gets a budget
    /// rather than an open bar. Four is enough to check a couple of sources and
    /// cross-check a cultivar name.
    /// </summary>
    private const int MaxWebSearches = 4;

    private readonly AnthropicClient _client;
    private readonly AiOptions _options;
    private readonly ILogger<AnthropicAiProvider> _logger;

    public string Name => AiProviderNames.Anthropic;

    public AnthropicAiProvider(IOptions<AiOptions> options, ILogger<AnthropicAiProvider> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new AiProviderException(
                "Anthropic API key is not configured.",
                ExceptionCodes.AI_PROVIDER_NOT_CONFIGURED,
                "Set Ai:ApiKey (env: Ai__ApiKey).");
        }

        _client = new AnthropicClient
        {
            ApiKey = _options.ApiKey,
            Timeout = TimeSpan.FromMinutes(_options.RequestTimeoutMinutes)
        };
    }

    public async Task<AiModels.AiResponse> CompleteAsync(AiModels.AiRequest request, CancellationToken cancellationToken)
    {
        List<MessageParam> messages = BuildMessages(request.Messages);
        var usage = new AiModels.AiUsage();

        // A server-side search can pause the turn part-way through. Resuming is
        // just "send the same request with the assistant's partial turn on the
        // end", and it is done here rather than above IAiProvider because the
        // blocks that have to go back are Anthropic's own — pushing them through
        // the neutral message model would mean flattening and rebuilding them.
        for (int resume = 0; ; resume++)
        {
            Message response = await SendAsync(request, messages, cancellationToken);

            usage.InputTokens += (int)(response.Usage?.InputTokens ?? 0);
            usage.OutputTokens += (int)(response.Usage?.OutputTokens ?? 0);
            usage.CacheReadTokens += (int)(response.Usage?.CacheReadInputTokens ?? 0);
            usage.CacheWriteTokens += (int)(response.Usage?.CacheCreationInputTokens ?? 0);

            bool paused = response.StopReason?.ToString() == "pause_turn";

            if (!paused || resume >= MaxPauseResumes)
            {
                if (paused)
                {
                    _logger.LogWarning("Anthropic paused the turn {Count} times without finishing", resume + 1);
                }

                AiModels.AiResponse mapped = MapResponse(response);
                mapped.Usage = usage;

                return mapped;
            }

            messages.Add(new MessageParam
            {
                Role = Role.Assistant,
                Content = ToParams(response.Content)
            });
        }
    }

    /// <summary>
    /// Response blocks as request blocks, to hand a paused turn back.
    ///
    /// Through JSON because that is the only conversion the SDK offers: the two
    /// unions are the same wire shape, and every variant that can appear here —
    /// text, thinking, server_tool_use, web_search_tool_result — round-trips
    /// unchanged. Mapping them by hand would mean rebuilding each result block
    /// field by field, and dropping the search results' encrypted_content would
    /// make the resumed request invalid.
    /// </summary>
    private static List<ContentBlockParam> ToParams(IReadOnlyList<ContentBlock> content)
    {
        string json = JsonSerializer.Serialize(content);

        return JsonSerializer.Deserialize<List<ContentBlockParam>>(json)
               ?? throw new AiProviderException(
                   "The AI provider paused mid-search and the turn could not be resumed.",
                   ExceptionCodes.AI_PROVIDER_FAILED);
    }

    private async Task<Message> SendAsync(AiModels.AiRequest request, List<MessageParam> messages, CancellationToken cancellationToken)
    {
        // MessageCreateParams is init-only, so everything is assembled in one
        // initializer rather than mutated afterwards.
        var parameters = new MessageCreateParams
        {
            Model = request.ModelOverride ?? _options.Model,
            MaxTokens = request.MaxTokens,
            Messages = messages,

            // The system prompt and tool list are the stable prefix of every
            // turn, so a breakpoint on the last system block caches both.
            System = string.IsNullOrWhiteSpace(request.SystemPrompt)
                ? null
                : new List<TextBlockParam>
                {
                    new()
                    {
                        Text = request.SystemPrompt,
                        CacheControl = new CacheControlEphemeral()
                    }
                },

            Tools = BuildTools(request, _options.WebSearch),

            OutputConfig = BuildOutputConfig()
        };

        try
        {
            // Streamed and reassembled, rather than awaited whole. A research
            // turn — high effort, several web searches — can spend minutes
            // producing nothing on a non-streaming connection, which is long
            // enough for the SDK's request timeout and for any idle timeout
            // between here and the API to give up on it. A stream that is
            // delivering keeps both alive. Nothing above IAiProvider consumes
            // deltas, so the events go straight back into the same Message a
            // non-streaming call would have returned.
            return await _client.Messages.CreateStreaming(parameters, cancellationToken).Aggregate();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The SDK's own timeout rather than the caller giving up. Left as a
            // raw OperationCanceledException it reaches the tablet as "The
            // operation was canceled.", which says nothing to the person
            // standing in front of it.
            _logger.LogError("Anthropic request timed out after {Minutes} minutes", _options.RequestTimeoutMinutes);

            throw new AiProviderException(
                "The AI provider took too long to answer.",
                ExceptionCodes.AI_PROVIDER_TIMEOUT,
                "Try again — a shorter brief usually comes back faster.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Anthropic request failed");

            throw new AiProviderException(
                "The AI provider request failed.",
                ExceptionCodes.AI_PROVIDER_FAILED,
                ex.Message);
        }
    }

    /// <summary>
    /// The caller's own tools, plus web search when it asked for it and the
    /// deployment allows it. Search is a server-side tool: Anthropic runs it and
    /// the results arrive in the same response, so nothing here executes
    /// anything.
    /// </summary>
    private static List<ToolUnion>? BuildTools(AiModels.AiRequest request, bool webSearchAllowed)
    {
        var tools = request.Tools.Select(t => new ToolUnion(BuildTool(t))).ToList();

        if (request.AllowWebSearch && webSearchAllowed)
        {
            tools.Add(new ToolUnion(new WebSearchTool20260209 { MaxUses = MaxWebSearches }));
        }

        return tools.Count > 0 ? tools : null;
    }

    /// <summary>
    /// Provider-specific knobs deliberately live in config rather than in
    /// IAiProvider — see the interface docs for why.
    /// </summary>
    private OutputConfig? BuildOutputConfig()
    {
        if (!_options.ProviderOptions.TryGetValue("effort", out string? effort) || string.IsNullOrWhiteSpace(effort))
        {
            return null;
        }

        return new OutputConfig
        {
            Effort = effort.ToLowerInvariant() switch
            {
                "low" => Effort.Low,
                "medium" => Effort.Medium,
                "high" => Effort.High,
                "max" => Effort.Max,
                _ => Effort.High
            }
        };
    }

    private static List<MessageParam> BuildMessages(List<AiModels.AiMessage> messages)
    {
        var result = new List<MessageParam>();

        foreach (AiModels.AiMessage message in messages)
        {
            switch (message.Role)
            {
                case AppEnums.AiMessageRole.User when message.Images.Count == 0:
                    result.Add(new MessageParam
                    {
                        Role = Role.User,
                        Content = message.Content ?? string.Empty
                    });
                    break;

                case AppEnums.AiMessageRole.User:
                    result.Add(new MessageParam
                    {
                        Role = Role.User,
                        Content = BuildUserContent(message)
                    });
                    break;

                case AppEnums.AiMessageRole.Assistant:
                    result.Add(new MessageParam
                    {
                        Role = Role.Assistant,
                        Content = BuildAssistantContent(message)
                    });
                    break;

                case AppEnums.AiMessageRole.Tool:
                    // All results for one assistant turn go back in a single
                    // user message — splitting them across messages trains the
                    // model out of parallel tool calls.
                    result.Add(new MessageParam
                    {
                        Role = Role.User,
                        Content = message.ToolResults
                            .Select(r => (ContentBlockParam)new ToolResultBlockParam
                            {
                                ToolUseID = r.ToolCallId,
                                Content = r.Content,
                                IsError = r.IsError
                            })
                            .ToList()
                    });
                    break;
            }
        }

        return result;
    }

    /// <summary>
    /// Images first, then the question. That is the order Anthropic documents as
    /// working best, and it reads the same way a person would look: at the thing,
    /// then at what is being asked about it.
    /// </summary>
    private static List<ContentBlockParam> BuildUserContent(AiModels.AiMessage message)
    {
        var blocks = new List<ContentBlockParam>();

        foreach (AiModels.AiImage image in message.Images)
        {
            blocks.Add(new ImageBlockParam
            {
                Source = new Base64ImageSource
                {
                    Data = image.Base64,
                    MediaType = image.MediaType
                }
            });
        }

        if (!string.IsNullOrWhiteSpace(message.Content))
        {
            blocks.Add(new TextBlockParam { Text = message.Content });
        }

        return blocks;
    }

    private static List<ContentBlockParam> BuildAssistantContent(AiModels.AiMessage message)
    {
        var blocks = new List<ContentBlockParam>();

        if (!string.IsNullOrWhiteSpace(message.Content))
        {
            blocks.Add(new TextBlockParam { Text = message.Content });
        }

        foreach (AiModels.AiToolCall call in message.ToolCalls)
        {
            blocks.Add(new ToolUseBlockParam
            {
                ID = call.Id,
                Name = call.Name,
                Input = ParseInput(call.JsonInput)
            });
        }

        // An assistant turn must not be empty; an all-whitespace reply would
        // otherwise 400 on the next request.
        if (blocks.Count == 0)
        {
            blocks.Add(new TextBlockParam { Text = "(no content)" });
        }

        return blocks;
    }

    private static Tool BuildTool(AiModels.AiToolDefinition definition)
    {
        using JsonDocument schema = JsonDocument.Parse(definition.InputSchema);

        var properties = new Dictionary<string, JsonElement>();

        if (schema.RootElement.TryGetProperty("properties", out JsonElement props))
        {
            foreach (JsonProperty property in props.EnumerateObject())
            {
                properties[property.Name] = property.Value.Clone();
            }
        }

        var required = new List<string>();

        if (schema.RootElement.TryGetProperty("required", out JsonElement requiredElement) &&
            requiredElement.ValueKind == JsonValueKind.Array)
        {
            required.AddRange(requiredElement.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString()!));
        }

        return new Tool
        {
            Name = definition.Name,
            Description = definition.Description,
            InputSchema = new InputSchema
            {
                Properties = properties,
                Required = required
            }
        };
    }

    private static Dictionary<string, JsonElement> ParseInput(string json)
    {
        var result = new Dictionary<string, JsonElement>();

        if (string.IsNullOrWhiteSpace(json))
        {
            return result;
        }

        using JsonDocument document = JsonDocument.Parse(json);

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        foreach (JsonProperty property in document.RootElement.EnumerateObject())
        {
            result[property.Name] = property.Value.Clone();
        }

        return result;
    }

    private static AiModels.AiResponse MapResponse(Message response)
    {
        var mapped = new AiModels.AiResponse
        {
            Model = response.Model.ToString() ?? string.Empty,
            StopReason = MapStopReason(response.StopReason?.ToString()),
            Usage = new AiModels.AiUsage
            {
                InputTokens = (int)(response.Usage?.InputTokens ?? 0),
                OutputTokens = (int)(response.Usage?.OutputTokens ?? 0),
                CacheReadTokens = (int)(response.Usage?.CacheReadInputTokens ?? 0),
                CacheWriteTokens = (int)(response.Usage?.CacheCreationInputTokens ?? 0)
            }
        };

        var text = new List<string>();

        foreach (ContentBlock block in response.Content)
        {
            if (block.TryPickText(out TextBlock? textBlock) && textBlock is not null)
            {
                text.Add(textBlock.Text);
            }
            else if (block.TryPickToolUse(out ToolUseBlock? toolUse) && toolUse is not null)
            {
                mapped.ToolCalls.Add(new AiModels.AiToolCall
                {
                    Id = toolUse.ID,
                    Name = toolUse.Name,
                    JsonInput = JsonSerializer.Serialize(toolUse.Input)
                });
            }
        }

        mapped.Content = text.Count > 0 ? string.Join("\n", text) : null;

        return mapped;
    }

    private static AppEnums.AiStopReason MapStopReason(string? stopReason) => stopReason switch
    {
        "end_turn" => AppEnums.AiStopReason.EndTurn,
        "tool_use" => AppEnums.AiStopReason.ToolUse,
        "max_tokens" => AppEnums.AiStopReason.MaxTokens,
        "refusal" => AppEnums.AiStopReason.Refusal,
        _ => AppEnums.AiStopReason.Other
    };
}
