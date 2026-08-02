using RandomTaskTrack.Data.Models.Ai;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Interfaces.Ai;

namespace RandomTaskTrack.Business.Ai.Providers;

/// <summary>
/// Registered when no AI provider is configured. Everything except chat still
/// works — the app boots, the dashboard renders, tasks can be ticked off — and
/// only a chat request produces a clear error instead of a startup crash.
/// </summary>
public class NullAiProvider : IAiProvider
{
    public string Name => AiProviderNames.Null;

    public Task<AiResponse> CompleteAsync(AiRequest request, CancellationToken cancellationToken)
    {
        throw new AiProviderException(
            "No AI provider is configured.",
            ExceptionCodes.AI_PROVIDER_NOT_CONFIGURED,
            "Set Ai:Provider and Ai:ApiKey (env: Ai__Provider, Ai__ApiKey) to enable chat.");
    }
}
