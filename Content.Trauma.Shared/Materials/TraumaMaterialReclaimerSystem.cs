// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Materials;
using Robust.Shared.Physics.Events;

namespace Content.Trauma.Shared.Materials;

/// <summary>
/// Makes it so when you stop colliding with an emagged recycler it won't just mince you forever remotely.
/// </summary>
public sealed partial class TraumaMaterialReclaimerSystem : EntitySystem
{
    [Dependency] private EntityQuery<ActiveMaterialReclaimerComponent> _activeQuery = default!;

    [SubscribeLocalEvent]
    private void OnEndCollide(Entity<CollideMaterialReclaimerComponent> ent, ref EndCollideEvent args)
    {
        if (args.OurFixtureId != ent.Comp.FixtureId || !_activeQuery.TryComp(ent, out var active))
            return;

        active.Processing.Remove(args.OtherEntity);
    }
}
