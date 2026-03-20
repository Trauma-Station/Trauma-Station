// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared.StatusIcon.Components;
using Content.Trauma.Shared.Heretic.Systems;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Client.Heretic.Systems;

public sealed class GhoulSystem : SharedGhoulSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetStatusIconsEvent>(OnGetIcons);
    }

    private void OnGetIcons(ref GetStatusIconsEvent args)
    {
        if (_player.LocalEntity is not { } player)
            return;

        if (TryComp(player, out HereticMinionComponent? minion))
        {
           if (minion.BoundHeretic == args.Uid)
               args.StatusIcons.Add(_prototype.Index(minion.MasterIcon));

           if (TryComp(args.Uid, out HereticMinionComponent? minion2) && minion2.MinionId == minion.MinionId)
               args.StatusIcons.Add(_prototype.Index(minion.GhoulIcon));
        }
        else if (TryComp(args.Uid, out HereticMinionComponent? minion2) && minion2.BoundHeretic == player)
            args.StatusIcons.Add(_prototype.Index(minion2.GhoulIcon));
    }
}
