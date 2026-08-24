using Content.Shared.Storage.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Storage.EntitySystems;

public sealed partial class EntityProviderSystem
{
    /// <summary>
    /// Adds a prototype to the provider.
    /// </summary>
    public void AddToProvider(Entity<EntityProviderComponent?> ent, [ForbidLiteral] EntProtoId id, EntityUid? user = null)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!ent.Comp.EntityCounter.TryAdd(id, 1))
            ent.Comp.EntityCounter[id]++;

        if (user != null)
            _audio.PlayPredicted(ent.Comp.SingularTransferSound, ent, user.Value);

        Dirty(ent, ent.Comp);
    }
}
