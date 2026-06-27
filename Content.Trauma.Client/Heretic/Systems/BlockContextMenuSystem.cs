// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;
using Content.Trauma.Common.Heretic;
using Content.Trauma.Shared.Heretic.Components;

namespace Content.Trauma.Client.Heretic.Systems;

public sealed partial class BlockContextMenuSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlockContextMenuComponent, ShouldBlockContextMenuEvent>(OnShouldBlock);
    }

    private void OnShouldBlock(Entity<BlockContextMenuComponent> ent, ref ShouldBlockContextMenuEvent args)
    {
        if (_whitelist.CheckBoth(args.Target, ent.Comp.Blacklist, ent.Comp.Whitelist))
            args.ShouldBlock = true;
    }
}
