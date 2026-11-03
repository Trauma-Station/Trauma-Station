// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Ghost;
using Content.Shared.GameTicking;
using Content.Shared.Light.Components;
using Content.Shared.Station.Components;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Trauma.Server.CosmicCult;
using Content.Trauma.Server.CosmicCult.Components;
using Content.Trauma.Server.CosmicCult.EntitySystems;
using Content.Trauma.Shared.GameTicking.Rules;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Trauma.Server.CosmicCult;

public sealed partial class MalignRiftSpawnRule : StationEventSystem<MalignRiftSpawnRuleComponent>
{
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private ChatSystem _chatSystem = default!;
    [Dependency] private CosmicCultRuleSystem _cultRule = default!;
    [Dependency] private GhostSystem _ghost = default!;
    [Dependency] private IRobustRandom _rand = default!;
    [Dependency] private IPlayerManager _playerMan = default!;

    protected override void Started(Entity<MalignRiftSpawnRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        var (uid, comp, rule) = ent;
        if (_ticker.IsGameRuleActive<CosmicCultRuleComponent>())
        {
            _ticker.EndGameRule((uid, rule)); // Cosmic cult's active! Don't actually proceed to the contents of the gamerule!
            return;
        }

        var totalCrew = _playerMan.Sessions.Count(session => session.Status == SessionStatus.InGame && HasComp<HumanoidProfileComponent>(session.AttachedEntity));

        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("cosmiccult-announce-tier2-warning"), null, true, comp.Tier2Sound, Color.FromHex("#cae8e8"));

        var lights = EntityQueryEnumerator<PoweredLightComponent>();
        while (lights.MoveNext(out var light, out _))
        {
            if (!_rand.Prob(0.60f))
                continue;
            _ghost.DoGhostBooEvent(light);
        }

        for (var i = 0; i < Convert.ToInt16(totalCrew / 6); i++) // spawn # malign rifts equal to 16.67% of the playercount
        {
            // TODO: limit it to the relevant station if that gets made
            _cultRule.SpawnRift();
        }
    }
}
