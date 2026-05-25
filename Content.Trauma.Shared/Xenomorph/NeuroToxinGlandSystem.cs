// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Events;
using Content.Trauma.Shared.Xenomorphs;

namespace Content.Trauma.Shared.Xenomorph;

public sealed partial class NeurotoxinGlandSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, ShotAttemptedEvent>(_body.RelayEvent);
        SubscribeLocalEvent<NeurotoxinGlandComponent, ToggleAcidSpitEvent>(OnToggleAcidSpit);
        SubscribeLocalEvent<NeurotoxinGlandComponent, BodyRelayedEvent<ShotAttemptedEvent>>(OnShotAttempted);
    }

    private void OnShotAttempted(Entity<NeurotoxinGlandComponent> ent, ref BodyRelayedEvent<ShotAttemptedEvent> args)
    {
        if (ent.Comp.Active)
            return;

        // Prevent shooting if the gland is not active. It still lets them shove.
        var ev = args.Args;
        ev.Cancel();
        args.Args = ev; // holy dogshit please never ever do this
    }

    private void OnToggleAcidSpit(Entity<NeurotoxinGlandComponent> ent, ref ToggleAcidSpitEvent args)
    {
        // Toggle the active state
        ent.Comp.Active = !ent.Comp.Active;
        _popup.PopupPredicted(Loc.GetString(ent.Comp.Active ? "neurotoxin-gland-activated" : "neurotoxin-gland-deactivated"), args.Performer, args.Performer);
        Dirty(ent);
        args.Handled = true;
    }
}
