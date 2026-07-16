// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.Items;

public sealed partial class RestrictedMeleeSystem : EntitySystem
{
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private SharedAudioSystem _audioSystem = default!;
    [Dependency] private EntityWhitelistSystem _entityWhitelist = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RestrictedMeleeComponent, AttemptMeleeEvent>(OnMeleeAttempt);
    }

    private bool CanUse(EntityUid uid, RestrictedMeleeComponent comp) => comp.Whitelist != null && _entityWhitelist.IsValid(comp.Whitelist, uid);

    private void OnMeleeAttempt(EntityUid uid, RestrictedMeleeComponent comp, ref AttemptMeleeEvent args)
    {
        // Specism.
        if (CanUse(args.User, comp))
            return;

        args.Message = Loc.GetString(comp.FailText, ("item", uid));

        if (comp.DoKnockdown)
            _stun.TryKnockdown(args.User, comp.KnockdownDuration, true);

        if (comp.ForceDrop)
            _hands.TryDrop(args.User);

        if (!_standing.IsDown(args.User))
            _audioSystem.PlayPredicted(comp.FallSound, args.User, args.User);

        // Display the message to the player and cancel the melee attempt.
        _popupSystem.PopupClient(args.Message, uid, PopupType.Large);
        args.Cancelled = true;
    }
}
