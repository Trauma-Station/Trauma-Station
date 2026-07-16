// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.GameTicking;
using Content.Shared.Popups;
using Content.Trauma.Common.CCVar;
using Content.Trauma.Shared.Antag;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Trauma.Server.Antag;

public sealed partial class AntagSummonerSystem : SharedAntagSummonerSystem
{
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private ISharedPlayerManager _player = default!;

    private int _minPlayers;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, TraumaCVars.AntagSummonerMinPlayers, x => _minPlayers = x, true);
    }

    protected override bool TrySummonAntag(Entity<AntagSummonerComponent> ent, EntityUid user)
    {
        // TODO: % alive check too if people are extra chuddy
        if (_player.PlayerCount < _minPlayers)
        {
            Popup.PopupEntity("Security grants are temporarily unavailable, please try again later.", ent, user, PopupType.SmallCaution);
            return false;
        }

        _ticker.StartGameRule(ent.Comp.GameRule);
        return true;
    }
}
