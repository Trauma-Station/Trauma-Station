// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Interaction;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Popups;

namespace Content.Trauma.Shared.Interaction;

public sealed class UseRequiresSightSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private EntityQuery<BlindableComponent> _query;

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<BlindableComponent>();

        SubscribeLocalEvent<UseRequiresSightComponent, UseInHandAttemptEvent>(OnUseAttempt);
    }

    private void OnUseAttempt(Entity<UseRequiresSightComponent> ent, ref UseInHandAttemptEvent args)
    {
        var user = args.User;
        if (args.Cancelled || !_query.TryComp(user, out var comp) || !comp.IsBlind)
            return;

        _popup.PopupClient(Loc.GetString("blindness-fail-attempt"), user, user);
        args.Cancelled = true;
    }
}
