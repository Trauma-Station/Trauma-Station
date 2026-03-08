// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Random.Helpers;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.EntityEffects;

public sealed partial class StealItem : EntityEffectBase<StealItem>
{
    /// <summary>
    /// Chance to steal an item out of hands.
    /// </summary>
    [DataField]
    public float Chance = 1.0f;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => null;
}

public sealed class StealItemSystem : EntityEffectSystem<HandsComponent, StealItem>
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedWieldableSystem _wield = default!;

    protected override void Effect(Entity<HandsComponent> ent, ref EntityEffectEvent<StealItem> args)
    {
        if (args.User is not { } user)
            return;

        var prob = 0.5f * args.Scale;
        if (SharedRandomExtensions.PredictedProb(_timing, prob, GetNetEntity(user)))
            return;

        if (!HasComp<HandsComponent>(user))
            return;

        // prioritize active item, but fall back to the first one
        if (!_hands.TryGetActiveItem(ent.AsNullable(), out var item))
        {
            foreach (var hand in ent.Comp.Hands)
            {
                if (_hands.TryGetHeldItem(ent.AsNullable(), hand.Key, out item))
                    break;
            }
        }

        if (item is not { } stolen)
            return;

        if (TryComp<WieldableComponent>(ent, out var wield))
            _wield.TryUnwield(steal, wield, ent, true);

        if (!_hands.TryDrop(ent.AsNullable(), stolen))
            return;

        _hands.TryPickupAnyHand(user, stolen);
    }
}
