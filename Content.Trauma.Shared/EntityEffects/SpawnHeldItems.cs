// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Spawns some items into the target's hands, or drops them on the floor if they're full.
/// </summary>
public sealed partial class SpawnHeldItems : EntityEffectBase<SpawnHeldItems>
{
    [DataField(required: true)]
    public List<EntProtoId> Items;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class SpawnHeldItemsSystem : EntityEffectSystem<HandsComponent, SpawnHeldItems>
{
    [Dependency] private SharedHandsSystem _hands = default!;

    protected override void Effect(Entity<HandsComponent> ent, ref EntityEffectEvent<SpawnHeldItems> args)
    {
        var coords = Transform(ent).Coordinates;
        foreach (var id in args.Effect.Items)
        {
            var item = PredictedSpawnAtPosition(id, coords);
            _hands.TryPickupAnyHand(ent, item, checkActionBlocker: false, handsComp: ent.Comp);
        }
    }
}
