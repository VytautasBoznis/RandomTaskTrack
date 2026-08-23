using RandomTaskTrack.Data.Models.Recipes;
using RandomTaskTrack.Interfaces.Base;

namespace RandomTaskTrack.Interfaces.Services;

public interface IRecipePicker
{
    /// <summary>
    /// Pulls a dish and records it as the pick for that week. Shared by the
    /// first load of the week and by a reroll.
    /// </summary>
    /// <param name="familyId">Null rotates to the least recently used family.</param>
    Task<RecipePick> PickAsync(DateOnly weekOf, int? familyId, IUnitOfWork unitOfWork, CancellationToken cancellationToken);
}
