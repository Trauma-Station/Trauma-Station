// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Interaction;
using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.Interaction;

public sealed partial class UseWhitelistSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UseWhitelistComponent, UseInHandAttemptEvent>(OnUseAttempt);
    }

    private void OnUseAttempt(Entity<UseWhitelistComponent> ent, ref UseInHandAttemptEvent args)
    {
        var user = args.User;
        if (args.Cancelled || _whitelist.IsWhitelistPass(ent.Comp.Whitelist, user))
            return;

        args.Cancelled = true;
    }
}
