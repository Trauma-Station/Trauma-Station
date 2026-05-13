// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.StatusEffects;
using Content.Trauma.Shared.Heretic.Events;

namespace Content.Trauma.Shared.Heretic.Systems;

public sealed partial class HereticAuraSystem : EntitySystem
{
    [Dependency] private SharedHereticSystem _heretic = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HideHereticAuraComponent, ComponentStartup>((uid, _, _) => _heretic.RemoveAura(uid));
        SubscribeLocalEvent<HideHereticAuraComponent, ComponentShutdown>((uid, _, _) =>
            _heretic.UpdateHereticAura(uid));
        SubscribeLocalEvent<HideHereticAuraComponent, ClothingGotEquippedEvent>((_, _, ev) =>
            _heretic.RemoveAura(ev.Wearer));
        SubscribeLocalEvent<HideHereticAuraComponent, ClothingGotUnequippedEvent>((_, _, ev) =>
            _heretic.UpdateHereticAura(ev.Wearer));

        Subs.SubscribeWithRelay<HideHereticAuraComponent, ShouldHideHereticAuraEvent>(OnHide, held: false);

        SubscribeLocalEvent<InventoryComponent, ShouldHideHereticAuraEvent>(_inventory.RelayEvent);

        SubscribeLocalEvent<StatusEffectContainerComponent, ShouldHideHereticAuraEvent>(_status.RelayEvent);

        SubscribeLocalEvent<HideHereticAuraStatusEffectComponent, StatusEffectRelayedEvent<ShouldHideHereticAuraEvent>>(
            OnStatusHide);
        SubscribeLocalEvent<HideHereticAuraStatusEffectComponent, StatusEffectAppliedEvent>((_, _, ev) =>
            _heretic.RemoveAura(ev.Target));
        SubscribeLocalEvent<HideHereticAuraStatusEffectComponent, StatusEffectRemovedEvent>((_, _, ev) =>
            _heretic.UpdateHereticAura(ev.Target));
    }

    private void OnStatusHide(Entity<HideHereticAuraStatusEffectComponent> ent,
        ref StatusEffectRelayedEvent<ShouldHideHereticAuraEvent> args)
    {
        if (ent.Comp.LifeStage > ComponentLifeStage.Running)
            return;

        args.Args = args.Args with { Hide = true };
    }

    private void OnHide(Entity<HideHereticAuraComponent> ent, ref ShouldHideHereticAuraEvent args)
    {
        if (ent.Comp.LifeStage > ComponentLifeStage.Running)
            return;

        args.Hide = true;
    }
}
