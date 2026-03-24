// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._White.Xenomorphs;
using Content.Shared.Body;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Trauma.Shared.Xenomorph;

public sealed class NeurotoxinGlandSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, ShotAttemptedEvent>(_body.RefRelayBodyEvent);
        SubscribeLocalEvent<NeurotoxinGlandComponent, ToggleAcidSpitEvent>(OnToggleAcidSpit);
        SubscribeLocalEvent<NeurotoxinGlandComponent, BodyRelayedEvent<ShotAttemptedEvent>>(OnShotAttempted);
    }

    private void OnShotAttempted(Entity<NeurotoxinGlandComponent> ent, ref BodyRelayedEvent<ShotAttemptedEvent> args)
    {
        // Prevent shooting if the gland is not active. It still lets them shove.
        if (!ent.Comp.Active)
            args.Args.Cancel();
    }

    private void OnToggleAcidSpit(Entity<NeurotoxinGlandComponent> ent, ref ToggleAcidSpitEvent args)
    {
        // Toggle the active state
        ent.Comp.Active = !ent.Comp.Active;
        _popup.PopupPredicted(Loc.GetString(ent.Comp.Active ? "neurotoxin-gland-activated" : "neurotoxin-gland-deactivated"), args.Performer, args.Performer);
        Dirty(ent);
    }
}
