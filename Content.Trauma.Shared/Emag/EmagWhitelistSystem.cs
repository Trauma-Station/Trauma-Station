// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Trauma.Common.Emag;
using Content.Shared.Popups;
using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.Emag;

public sealed class EmagWhitelistSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmagWhitelistComponent, EmagAttemptEvent>(OnEmagAttempt);
    }

    private void OnEmagAttempt(Entity<EmagWhitelistComponent> ent, ref EmagAttemptEvent args)
    {
        if (args.Cancelled || _whitelist.IsWhitelistPass(ent.Comp.Whitelist, args.Target))
            return;

        var user = args.User;
        _popup.PopupClient(Loc.GetString("emag-attempt-failed", ("tool", ent)), user, user);
        args.Cancelled = true;
    }
}
