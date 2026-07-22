// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Dragon;
using Content.Server.GameTicking;
using Content.Server.RoundEnd;
using Content.Shared.Destructible;
using Content.Shared.GameTicking;

namespace Content.Trauma.Server.GameTicking.Rules.Components;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class TraumaDragonRuleSystem : EntitySystem
{
    [Dependency] private RoundEndSystem _roundEnd = default!;

    private int _portals;
    public override void Initialize()
    {
        SubscribeLocalEvent<DragonRiftComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DragonRiftComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRestart);
    }

    private void OnRestart(RoundRestartCleanupEvent ev)
    {
        _portals = 0;
    }

    private void OnMapInit(Entity<DragonRiftComponent> ent, ref MapInitEvent args)
    {
        _portals++;

        if (_portals > 2)
            _roundEnd.RequestRoundEnd(countdownTime: TimeSpan.FromMinutes(5));
    }

    private void OnDestruction(Entity<DragonRiftComponent> ent, ref DestructionEventArgs args)
    {
        _portals--;
    }
}
