// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Trauma.Shared.StatusEffects;

public sealed class UnableToShootStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectContainerComponent, ShotAttemptedEvent>(_statusEffects.RefRelayStatusEffectEvent);

        SubscribeLocalEvent<UnableToShootStatusEffectComponent, StatusEffectRelayedEvent<ShotAttemptedEvent>>(OnAttemptShoot);
    }

    private void OnAttemptShoot(Entity<UnableToShootStatusEffectComponent> ent, ref StatusEffectRelayedEvent<ShotAttemptedEvent> args)
    {
        _popup.PopupClient("Your fingers slip!", ent.Owner, ent.Owner);

        var ev = args.Args;
        ev.Cancel();
        args.Args = ev;
    }
}
