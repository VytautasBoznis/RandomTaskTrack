using RandomTaskTrack.Data.Models.Ai;
using RandomTaskTrack.Data.Models.Plants;

namespace RandomTaskTrack.Interfaces.Plants;

/// <summary>
/// "Here is a plant I own — what is it and how do I keep it alive?", and the
/// follow-up question every photo after the first one asks: "how is it doing?"
///
/// One shot each, no tools of ours, no conversation: the answers are a profile
/// and a stage, not a chat. Kept behind an interface for the same reason
/// IStockPriceSource is — swapping the model for a plant database later should
/// be one class.
/// </summary>
public interface IPlantResearcher
{
    Task<PlantResearchResult> ResearchAsync(PlantResearchQuestion question, CancellationToken cancellationToken);

    /// <summary>
    /// Reads a progress photo: what stage it shows, and anything visibly wrong.
    /// </summary>
    /// <param name="what">The plant's name and species, so the answer is about
    /// this plant rather than about whatever the picture resembles.</param>
    Task<PlantStageRead> ReadStageAsync(Plant what, AiImage image, CancellationToken cancellationToken);
}
