// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Station.Systems;
using Content.Shared.Antag;
using Content.Shared.Antag.Components;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Rules.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Station.Components;
using Content.Trauma.Shared.BloodCult;
using Content.Trauma.Shared.BloodCult.Gamerule;
using Content.Trauma.Shared.BloodCult.Runes;
using Robust.Shared.Map;
using Robust.Shared.Player;
using System.Linq;

namespace Content.IntegrationTests.Tests._Trauma;

[Category("GameRuleTests")]
public sealed class BloodCultTest : InteractionTest
{
    private static readonly EntProtoId CultRuneOffering = "CultRuneOffering";
    private static readonly EntProtoId Dagger = "RitualDagger";
    private static readonly EntProtoId GameRule = "BloodCult";
    private static readonly EntProtoId TestStation = "TestStation";
    private static readonly EntProtoId Urist = "MobHuman";
    private static readonly ProtoId<AntagSpecifierPrototype> CultistSpecifier = "BloodCultist";

    protected override string PlayerPrototype => Urist; // needs bloodstream and stuff

    [SidedDependency(Side.Server)] private AntagSelectionSystem _antag = default!;
    [SidedDependency(Side.Server)] private CultRuneSystem _rune = default!;
    [SidedDependency(Side.Server)] private GameTicker _ticker = default!;
    [SidedDependency(Side.Server)] private ServerStationSystem _station = default!;
    [SidedDependency(Side.Server)] private SharedMindSystem _mind = default!;

    [Test]
    public async Task CultConversionTest()
    {
        var coords = SEntMan.GetCoordinates(PlayerCoords);

        var specifier = SProtoMan.Index(CultistSpecifier);

        var dummies = await Server.AddDummySessions(2);
        await Server.WaitPost(() =>
        {
            var helper = SpawnWithMind(dummies[0], coords);

            var (antag, rule) = StartRule();

            // spawned after starting the rule so its not picked to be sacrificed
            var initiate = SpawnWithMind(dummies[1], coords);

            Assert.That(_antag.IsEntityValid(SPlayer, specifier), "Player entity somehow wasnt valid");
            Assert.That(_antag.IsSessionValid(ServerSession, antag, specifier), "Session wasnt valid for some reason");

            Assert.That(_antag.TryAssignNextAvailableAntag(antag, ServerSession, checkPref: false), "Couldn't give the main player cultist antag");
            Assert.That(_antag.TryAssignNextAvailableAntag(antag, dummies[0], checkPref: false), "Couldn't give the helper player cultist antag");

            var cultistMinds = _antag.GetAntagIdentifiers(antag).Select(tuple => tuple.Item1).ToList();
            Assert.That(cultistMinds.Count, Is.EqualTo(2), "Wrong number of cultists picked, expected 1 leader and 1 cultist");

            Assert.That(SHasComp<BloodCultistComponent>(SPlayer), "Main player did not get cultist");
            Assert.That(SHasComp<BloodCultistComponent>(helper), "Helper player did not cultist");
            Assert.That(!SHasComp<BloodCultistComponent>(initiate), "Initiate player wrongly got cultist");
            Assert.That(SComp<BloodCultMemberComponent>(SPlayer).Rule, Is.EqualTo(antag.Owner));

            Assert.That(_mind.GetMind(initiate), Is.Not.Null, "Initiate had no mind");
            Assert.That(initiate, Is.Not.EqualTo(rule.OfferingTarget), "Can't convert the sacrifice target!");

            // spawn the rune to convert with
            var rune = SSpawn(CultRuneOffering, coords);
            var runeComp = SComp<CultRuneComponent>(rune);

            // convert the inititate
            Assert.That(_rune.InvokeRune((rune, runeComp), SPlayer), Is.Null, "Failed to invoke the offering rune!");

            Assert.That(SHasComp<BloodCultistComponent>(initiate));

            Assert.That(_antag.GetAntagIdentifiers(antag).Count(), Is.EqualTo(3), "Conversion didn't work");

            SDel(_mind.GetMind(initiate));
            SDel(_mind.GetMind(helper));
            SDel(_mind.GetMind(SPlayer));
            SDel(rune);
            SDel(initiate);
            SDel(helper);
            SDel(antag);
        });

        /*
        var dagger = SSpawn(Dagger, coords);
        Hands.TryPickupAnyHand(cultist, dagger);

        Assert.That(SEntMan.Count<CultRuneDrawingComponent>(), Is.Zero);

        // start the rune

        Assert.That(SEntMan.Count<CultRuneComponent>(), Is.Zero);
        Assert.That(SEntMan.Count<CultRuneDrawingComponent>(), Is.EqualTo(1), "drawing rune should have worked");

        // draw it

        Assert.That(SEntMan.Count<CultRuneComponent>(), Is.EqualTo(1), "drawing rune should have worked");
        var rune = GetFirst<CultRuneComponent>();
        */
    }

    /// <summary>
    /// Checks that a cult starts with an offering target and enough ritual sites.
    /// </summary>
    [Test]
    public async Task CultStartTest()
    {
        var coords = SEntMan.GetCoordinates(PlayerCoords);

        await Server.WaitPost(() =>
        {
            var mind = AssignMind(ServerSession, SPlayer);

            var (antag, rule) = StartRule();
            Assert.That(rule.OfferingTarget, Is.Not.Null, "Blood cult rule didn't pick an offering target");
            Assert.That(rule.RitualAreas.Count, Is.EqualTo(rule.AreaCount), "Blood cult rule didn't start with enough ritual areas");
            SDel(mind);
            SDel(antag);
        });
    }

    /// <summary>
    /// Verifies that the cult can sacrifice their target and the gamerule counts it.
    /// </summary>
    [Test]
    public async Task CultSacrificeTest()
    {
        var coords = SEntMan.GetCoordinates(PlayerCoords);

        var specifier = SProtoMan.Index(CultistSpecifier);

        var dummies = await Server.AddDummySessions(3);
        await Server.WaitPost(() =>
        {
            var mind = AssignMind(ServerSession, SPlayer); // cap his ass

            var (antag, rule) = StartRule();
            Assert.That(rule.OfferingTarget, Is.EqualTo(SPlayer), "Rule should have picked the only player as the target");
            Assert.That(!rule.TargetSacrificed, "Target should not start as sacrificed");

            // now add the evil cultists
            var mobs = new EntityUid[dummies.Length];
            for (var i = 0; i < dummies.Length; i++)
            {
                var mob = SpawnWithMind(dummies[i], coords);
                // force set it since the test playercount is too low for 3 cultists naturally
                _antag.ForceMakeAntag(dummies[i], antag, specifier);
                Assert.That(SHasComp<BloodCultistComponent>(mob), $"Dummy {i} should be a cultist");
                mobs[i] = mob;
            }

            Assert.That(_antag.GetAntagIdentifiers(antag).Count(), Is.EqualTo(3), "Wrong number of cultists picked, expected 1 leader and 2 cultists");

            Assert.That(!SHasComp<BloodCultistComponent>(SPlayer), "Sac target should not be a cultist");

            // spawn the rune to sac with
            var rune = SSpawn(CultRuneOffering, coords);
            var runeComp = SComp<CultRuneComponent>(rune);

            // hit hard ye
            Assert.That(_rune.InvokeRune((rune, runeComp), SPlayer), Is.Not.Null, "The target shouldn't be able to sacrifice himself");
            Assert.That(_rune.InvokeRune((rune, runeComp), mobs[0]), Is.Null, "Failed to invoke the offering rune!");

            Assert.That(rule.TargetSacrificed, "Gamerule didn't recgonize the target being sacrificedn");

            Assert.That(_antag.GetAntagIdentifiers(antag).Count(), Is.EqualTo(3), "Sacrifice should not have converted");

            foreach (var mob in mobs)
            {
                SDel(_mind.GetMind(mob));
                SDel(mob);
            }
            SDel(_mind.GetMind(SPlayer));
            SDel(rune);
            SDel(antag);
        });
    }

    private EntityUid AssignMind(ICommonSession session, EntityUid mob)
    {
        var mind = _mind.CreateMind(session.UserId);
        _mind.TransferTo(mind, mob);
        Assert.That(session.AttachedEntity, Is.EqualTo(mob));
        Assert.That(SHasComp<ActorComponent>(mob));
        Assert.That(SHasComp<HumanoidProfileComponent>(mob));
        Assert.That(_mind.GetMind(mob), Is.EqualTo(mind.Owner));
        return mind;
    }

    private EntityUid SpawnWithMind(ICommonSession session, EntityCoordinates coords)
    {
        var mob = SSpawn(Urist, coords);
        AssignMind(session, mob);
        return mob;
    }

    private (Entity<AntagSelectionComponent>, BloodCultRuleComponent) StartRule()
    {
        // dummy station for the rule to use, needs comp for the event to target it
        var station = SSpawn(TestStation);
        _station.AddGridToStation(station, MapData.Grid);
        SEnsureComp<StationEventEligibleComponent>(station);

        if (!_ticker.StartGameRule(GameRule, out var rule))
            throw new Exception($"Failed to start gamerule {GameRule}");

        var antag = (rule.Value.Owner, SComp<AntagSelectionComponent>(rule.Value));
        var ruleComp = SComp<BloodCultRuleComponent>(rule.Value);
        return (antag, ruleComp);
    }

    private Entity<T> GetFirst<T>() where T : IComponent
    {
        var query = SEntMan.EntityQueryEnumerator<T>();
        foreach (var ent in query)
        {
            return ent;
        }

        throw new Exception($"Missing an entity with {typeof(T)}!");
    }
}
