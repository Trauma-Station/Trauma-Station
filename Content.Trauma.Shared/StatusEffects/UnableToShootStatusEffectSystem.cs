// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Trauma.Shared.StatusEffects;

public sealed partial class UnableToShootStatusEffectSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectContainerComponent, ShotAttemptedEvent>(_statusEffects.RelayEvent);

        SubscribeLocalEvent<UnableToShootStatusEffectComponent, StatusEffectRelayedEvent<ShotAttemptedEvent>>(OnAttemptShoot);
    }

    private void OnAttemptShoot(Entity<UnableToShootStatusEffectComponent> ent, ref StatusEffectRelayedEvent<ShotAttemptedEvent> args)
    {
        var user = args.Args.User;
        _popup.PopupEntity("Your fingers slip!", user, user);

        var ev = args.Args;
        ev.Cancel();
        args.Args = ev;
    }
}
