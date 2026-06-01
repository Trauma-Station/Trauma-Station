// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Vampires.Lair;

public sealed partial class VampireLairSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private static readonly ProtoId<DamageTypePrototype> Heat = "Heat";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireLairComponent, DamageDealtEvent>(OnDamage);

        SubscribeLocalEvent<VampireLairComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<VampireLairComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnDamage(Entity<VampireLairComponent> ent, ref DamageDealtEvent args)
    {
        // Only heat can damage this entity, and we don't want to notify the vampire for non-heat damage types cause its gonna spam
        if (ent.Comp.Vampire is not { } vamp || !args.Damage.DamageDict.ContainsKey(Heat))
            return;

        // Cooldown so the vampire doesn't get spammed with popups since fire damage gets dealt a lot of times
        var now = _timing.CurTime;
        if (now < ent.Comp.NextPopup)
            return;

        ent.Comp.NextPopup = ent.Comp.PopupCooldown + now;
        Dirty(ent);

        if (_net.IsServer)
            _popup.PopupEntity("Your lair is being attacked!", vamp, vamp, PopupType.LargeCaution);
    }

    private void OnInserted(Entity<VampireLairComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_timing.ApplyingState)
            return;

        if (ent.Comp.Vampire is not { } vampire || args.Entity != vampire)
            return;

        _status.TryAddStatusEffect(vampire, ent.Comp.CoffinStatus, out _);
    }

    private void OnRemoved(Entity<VampireLairComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (_timing.ApplyingState)
            return;

        if (ent.Comp.Vampire is not { } vampire || args.Entity != vampire)
            return;

        _status.TryRemoveStatusEffect(vampire, ent.Comp.CoffinStatus);
    }
}
