// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
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
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    protected override void Effect(Entity<HandsComponent> ent, ref EntityEffectEvent<StealItem> args)
    {
        var target = ent.Owner;
        if (args.User is not { } user)
            return;

        if (Random(user).NextFloat(0.0f, 1.0f) >= Math.Min(0.5f * args.Scale, 1f))
            return;

        if (!_hands.TryGetActiveItem(target, out var item) || (!HasComp<HandsComponent>(user)))
            return;

        if (!_hands.TryDrop(target, item.Value))
            return;

        _hands.TryPickup(user, item.Value);
    }

    public System.Random Random(EntityUid uid)
    {
        var seed = SharedRandomExtensions.HashCodeCombine((int) _timing.CurTick.Value, GetNetEntity(uid).Id);
        return new System.Random(seed);
    }
}
