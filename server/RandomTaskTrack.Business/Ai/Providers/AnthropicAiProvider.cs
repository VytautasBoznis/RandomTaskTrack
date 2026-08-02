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

        _client = new AnthropicClient { ApiKey = _options.ApiKey };
    }

    public async Task<AiModels.AiResponse> CompleteAsync(AiModels.AiRequest request, CancellationToken cancellationToken)
    {
        // MessageCreateParams is init-only, so everything is assembled in one
        // initializer rather than mutated afterwards.
        var parameters = new MessageCreateParams
        {
            Model = request.ModelOverride ?? _options.Model,
            MaxTokens = request.MaxTokens,
            Messages = BuildMessages(request.Messages),

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

            Tools = request.Tools.Count > 0
                ? request.Tools.Select(t => new ToolUnion(BuildTool(t))).ToList()
                : null,

            OutputConfig = BuildOutputConfig()
        };

        Message response;

        try
        {
            response = await _client.Messages.Create(parameters, cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Anthropic request failed");

            throw new AiProviderException(
                "The AI provider request failed.",
                ExceptionCodes.AI_PROVIDER_FAILED,
                ex.Message);
        }

        return MapResponse(response);
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
                case AppEnums.AiMessageRole.User:
                    result.Add(new MessageParam
                    {
                        Role = Role.User,
                        Content = message.Content ?? string.Empty
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
