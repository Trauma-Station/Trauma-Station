// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Coordinates;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
using Content.Trauma.Common.Nutrition;
using Content.Trauma.Shared.Ranching.Components;

namespace Content.Trauma.Shared.Ranching.Systems;

/// <summary>
/// Used for those stupid chickens that make me do more work.
/// </summary>
public sealed partial class SpecialEggsSystem : EntitySystem
{
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private EntityQuery<GlassChickenBlacklistComponent> _blacklistQuery = default!;

    [SubscribeLocalEvent]
    private void OnFullyAte(Entity<ChickenChestComponent> ent, ref FullyAteEvent args)
    {
        if (_blacklistQuery.HasComp(args.Food) ||
            Prototype(args.Food)?.ID is not { } proto)
            return;

        // TODO: use cloning
        var coords = Transform(ent).Coordinates;
        // Minecraft crazy craft chicken chest, if you know you know.
        PredictedSpawnAtPosition(proto, coords);
        PredictedSpawnAtPosition(proto, coords);
    }

    [SubscribeLocalEvent]
    private void OnInteract(Entity<PlateableChickenComponent> ent, ref InteractUsingEvent args)
    {
        if (!TryComp<PlateableChickenOreComponent>(args.Used, out var ore))
            return;

        EntityManager.AddComponents(ent.Owner, ore.Components);
        RemComp(ent, ent.Comp);

        if (_stack.GetCount(args.Used) > 0)
        {
            _stack.ReduceCount(args.Used, 1);
            return;
        }

        Del(args.Used);
    }
}
