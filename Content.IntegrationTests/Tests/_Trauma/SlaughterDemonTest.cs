// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Station;
using Content.Goobstation.Shared.SlaughterDemon;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Actions;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Mobs.Systems;
using Content.Shared.Station.Components;
using Content.Trauma.Server.Antag;
using Content.Trauma.Shared.Antag;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Trauma;

/// <summary>
/// Makes sure Slaughter Demon abilities and mechanics function correctly.
/// </summary>

[Category("GameRuleTests")]
public sealed class SlaughterDemonTest : InteractionTest
{
    public static readonly EntProtoId SlaughterDemon = "BaseMobSlaughterDemon";
    public static readonly EntProtoId Urist = "MobHuman";
    public static readonly EntProtoId BloodCrawlAction = "BloodCrawlAction";
    public static readonly ProtoId<AntagSmitePrototype> SmiteSlaughterDemon = "SlaughterDemon";
    public static readonly ProtoId<ReagentPrototype> Blood = "Blood";
    public static readonly string FloorSteel = "FloorSteel";

    protected override string PlayerPrototype => SlaughterDemon;

    [SidedDependency(Side.Server)] private AntagVerbSystem _smite = default!;
    [SidedDependency(Side.Server)] private PuddleSystem _puddle = default!;
    [SidedDependency(Side.Server)] private SharedActionsSystem _actions = default!;
    [SidedDependency(Side.Server)] private MobStateSystem _mobState = default!;
    [SidedDependency(Side.Server)] private ITileDefinitionManager _tileDef = default!;

    /// <summary>
    /// Verifies that a Slaughter Demon can enter blood puddles via Blood Jaunt (Blood Crawl) and can safely exit.
    /// </summary>
    [Test]
    public async Task BloodJauntWorks()
    {
        var demon = SPlayer;

        await Server.WaitPost(() =>
        {
            var mapSystem = SEntMan.System<SharedMapSystem>();
            var stationSystem = SEntMan.System<StationSystem>();
            var mapId = SEntMan.GetComponent<TransformComponent>(demon).MapID;

            var gridEnt = mapSystem.CreateGridEntity(mapId);

            var tileId = _tileDef[FloorSteel].TileId;
            mapSystem.SetTile(gridEnt, new Vector2i(0, 0), new Tile(tileId));
            var stationUid = SEntMan.SpawnEntity(null, MapCoordinates.Nullspace);
            var stationComp = SEntMan.AddComponent<StationDataComponent>(stationUid);
            var memberComp = SEntMan.AddComponent<StationMemberComponent>(gridEnt);

            memberComp.Station = stationUid;
            stationComp.Grids.Add(gridEnt);
        });

        await Server.WaitAssertion(() =>
        {
            _smite.MakeAntag(ServerSession!, SmiteSlaughterDemon);
            Assert.That(SEntMan.HasComponent<SlaughterDemonComponent>(demon),
                $"Slaughter Demon antag smite failed on {SEntMan.ToPrettyString(demon)}");
        });

        var puddleEnt = EntityUid.Invalid;
        await Server.WaitPost(() =>
        {
            var coords = SEntMan.GetComponent<TransformComponent>(demon).Coordinates;
            var solution = new Solution();
            solution.AddReagent(Blood, 100.0f);

            _puddle.TrySpillAt(coords, solution, out puddleEnt, sound: false);
            Assert.That(SEntMan.EntityExists(puddleEnt), "Failed to spawn blood puddle for Blood Jaunt test");
        });

        var actionEnt = EntityUid.Invalid;
        await Server.WaitAssertion(() =>
        {
            if (_actions.AddAction(demon, BloodCrawlAction) is { } act)
            {
                actionEnt = act;
            }
            Assert.That(SEntMan.EntityExists(actionEnt), "Failed to assign BloodCrawlAction to Slaughter Demon");
        });

        await Server.WaitAssertion(() =>
        {
            var targetNet = SEntMan.GetNetEntity(puddleEnt);
            var args = new RequestPerformActionEvent(SEntMan.GetNetEntity(actionEnt), targetNet);
            Assert.That(_actions.TryPerformAction(demon, args), "Failed to execute Blood Jaunt action on puddle");
        });

        await RunSeconds(1);

        await Server.WaitAssertion(() =>
        {
            var holder = SComp<BloodCrawlComponent>(demon);
            Assert.That(holder.IsCrawling, "Slaughter Demon failed to enter Blood Jaunt state");
        });

        await RunSeconds(10);

        await Server.WaitAssertion(() =>
        {
            var args = new RequestPerformActionEvent(SEntMan.GetNetEntity(actionEnt));
            Assert.That(_actions.TryPerformAction(demon, args), "Failed to exit Blood Jaunt action");
        });

        await RunSeconds(1);

        await Server.WaitAssertion(() =>
        {
            var holder = SComp<BloodCrawlComponent>(demon);
            Assert.That(!holder.IsCrawling, "Slaughter Demon failed to leave Blood Jaunt state");
            Assert.That(_mobState.IsAlive(demon), "Slaughter Demon should remain alive after exiting jaunt");
        });
    }
}
