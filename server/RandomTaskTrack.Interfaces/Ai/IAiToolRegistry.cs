using RandomTaskTrack.Data.Models.Ai;
using RandomTaskTrack.Interfaces.Base;

namespace RandomTaskTrack.Interfaces.Ai;

/// <summary>
/// The tools the AI may call, and their execution. Tool handlers run against
/// the same UnitOfWork as the surrounding operation, so a chat turn that
/// creates six tasks either commits fully or not at all.
/// </summary>
public interface IAiToolRegistry
{
    List<AiToolDefinition> GetDefinitions();

    Task<AiToolResult> ExecuteAsync(AiToolCall call, IUnitOfWork unitOfWork, CancellationToken cancellationToken);
}
