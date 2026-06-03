using Content.Shared.Cuffs;

namespace Content.Shared.Body;

public sealed partial class HandOrganSystem
{
    [Dependency] private SharedCuffableSystem _cuffable = default!;

    private void GiveStartingItem(Entity<HandOrganComponent> ent, EntityUid target)
    {
        if (ent.Comp.StartingItem is not { } proto)
            return;

        var item = PredictedSpawnNextToOrDrop(proto, target);
        _hands.TryPickup(target, item, ent.Comp.HandID, animate: false);
    }
}
