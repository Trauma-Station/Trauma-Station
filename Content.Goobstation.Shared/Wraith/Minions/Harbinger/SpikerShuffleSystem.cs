// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Shared.Wraith.Events;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Goobstation.Shared.Wraith.Minions.Harbinger;

public sealed partial class SpikerShuffleSystem : EntitySystem
{
    [Dependency] private Content.Shared.StatusEffect.StatusEffectsSystem _statusOld = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpikerShuffleComponent, SpikerShuffleEvent>(OnSpikerShuffle);

        SubscribeLocalEvent<SpikerShuffleEffectComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<SpikerShuffleEffectComponent, StatusEffectRemovedEvent>(OnRemoved);
    }

    private void OnSpikerShuffle(Entity<SpikerShuffleComponent> ent, ref SpikerShuffleEvent args)
    {
        // first remove all status effects
#pragma warning disable CS0618
        foreach (var statusEffect in ent.Comp.StatusEffectsToRemove)
            _statusOld.TryRemoveStatusEffect(ent.Owner, statusEffect);
#pragma warning restore CS0618
        foreach (var effect in ent.Comp.NewEffectsToRemove)
            _status.TryRemoveStatusEffect(ent.Owner, effect);

        _status.TryAddStatusEffect(ent.Owner, ent.Comp.StatusEffect, out _, ent.Comp.Duration);
        _status.TryAddStatusEffect(ent.Owner, ent.Comp.StatusAbilityDisable, out _, ent.Comp.Duration); // disable using actions

        args.Handled = true;
    }

    private void OnApplied(Entity<SpikerShuffleEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        var target = args.Target;
        _popup.PopupEntity(Loc.GetString("wraith-spiker-shuffle"), target, target, PopupType.Medium);
        _appearance.SetData(target, ShuffleVisuals.Shuffling, true);

        if (TryComp<FixturesComponent>(target, out var fixtures) && fixtures.FixtureCount >= 1)
        {
            var fixture = fixtures.Fixtures.First();

            _physics.SetCollisionMask(target, fixture.Key, fixture.Value, (int) CollisionGroup.SmallMobMask, fixtures);
            _physics.SetCollisionLayer(target, fixture.Key, fixture.Value, (int) CollisionGroup.SmallMobLayer, fixtures);
        }
    }

    private void OnRemoved(Entity<SpikerShuffleEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        var target = args.Target;
        _popup.PopupEntity(Loc.GetString("wraith-spiker-shuffle-removed"), target, target, PopupType.Medium);
        _appearance.SetData(target, ShuffleVisuals.Shuffling, false);

        if (TryComp<FixturesComponent>(target, out var fixtures) && fixtures.FixtureCount >= 1)
        {
            var fixture = fixtures.Fixtures.First();

            _physics.SetCollisionMask(target, fixture.Key, fixture.Value, (int) CollisionGroup.MobMask, fixtures);
            _physics.SetCollisionLayer(target, fixture.Key, fixture.Value, (int) CollisionGroup.MobLayer, fixtures);
        }
    }
}
