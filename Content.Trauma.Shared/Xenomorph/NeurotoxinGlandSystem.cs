// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._White.Xenomorphs;
using Content.Shared.Actions;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Trauma.Shared.Xenomorph;

public sealed class NeurotoxinGlandSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NeurotoxinGlandComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NeurotoxinGlandComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<NeurotoxinGlandComponent, ToggleAcidSpitEvent>(OnToggleAcidSpit);
        SubscribeLocalEvent<NeurotoxinGlandComponent, ShotAttemptedEvent>(OnShotAttempted);
    }

    private void OnMapInit(Entity<NeurotoxinGlandComponent> ent, ref MapInitEvent args) =>
        _actions.AddAction(ent.Owner, ent.Comp.ActionId);

    private void OnComponentShutdown(Entity<NeurotoxinGlandComponent> ent, ref ComponentShutdown args) =>
        _actions.RemoveAction(ent.Owner, ent.Comp.Action);

    private void OnShotAttempted(Entity<NeurotoxinGlandComponent> ent, ref ShotAttemptedEvent args)
    {
        // Prevent shooting if the gland is not active. It still lets them shove.
        if (!ent.Comp.Active)
            args.Cancel();
    }

    private void OnToggleAcidSpit(Entity<NeurotoxinGlandComponent> ent, ref ToggleAcidSpitEvent args)
    {
        // Toggle the active state
        ent.Comp.Active = !ent.Comp.Active;
        _popup.PopupPredicted(Loc.GetString(ent.Comp.Active ? "neurotoxin-gland-activated" : "neurotoxin-gland-deactivated"), ent.Owner, ent.Owner);
        Dirty(ent);
        args.Handled = true;
    }
}
