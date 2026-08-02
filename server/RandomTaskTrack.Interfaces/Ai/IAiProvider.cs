using RandomTaskTrack.Data.Models.Ai;

namespace RandomTaskTrack.Interfaces.Ai;

/// <summary>
/// The single seam every AI provider implements. Deliberately scoped to
/// "one chat completion, with tools" — the layer where Anthropic, OpenAI and
/// local models genuinely agree.
///
/// Things that are NOT here on purpose: thinking/reasoning configuration,
/// effort levels, prompt caching. Those differ enough between providers that
/// forcing them into a shared shape produces a lowest-common-denominator
/// abstraction. They live in AiOptions.ProviderOptions and are read by the
/// adapter that understands them.
/// </summary>
public interface IAiProvider
{
    /// <summary>Matches AiOptions.Provider. Used to pick the implementation.</summary>
    string Name { get; }

    Task<AiResponse> CompleteAsync(AiRequest request, CancellationToken cancellationToken);
}
