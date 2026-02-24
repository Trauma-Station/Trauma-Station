// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Makes the target drop a random item, then run entity effects on it.
/// The effects have this mob set as the user.
/// </summary>
public sealed partial class DropRandomItem : EntityEffectBase<DropRandomItem>
{
    /// <summary>
    /// Effects to run on the item after dropping it.
    /// </summary>
    [DataField]
    public EntityEffect[] Effects = [];

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null; // not used by reagents idc
}

public sealed class DropRandomItemEffectSystem : EntityEffectSystem<HandsComponent, DropRandomItem>
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    private List<EntityUid> _items = new();

    protected override void Effect(Entity<HandsComponent> ent, ref EntityEffectEvent<DropRandomItem> args)
    {
        _items.Clear();
        foreach (var held in _hands.EnumerateHeld(ent.AsNullable()))
        {
            _items.Add(held);
        }

        if (_items.Count == 0)
            return;

        // TODO: PredictedRandom when it's real
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new Random(seed);
        var item = rand.Pick(_items);
        if (!_hands.TryDrop(ent.AsNullable(), item)) // glued etc
            return;

        _effects.ApplyEffects(item, args.Effect.Effects, user: args.User ?? ent.Owner);
    }
}
