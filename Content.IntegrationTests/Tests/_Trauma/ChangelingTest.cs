// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Goobstation.Shared.Changeling.Components;
using Content.Medical.Common.Targeting;
using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.StatusEffectNew;
using Content.Trauma.Server.Antag;
using Content.Trauma.Shared.Antag;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Trauma;

public sealed partial class ChangelingTest : GameTest
{
    private static ProtoId<AntagSmitePrototype> Smite = "Changeling";
    private static ProtoId<DamageTypePrototype> Blunt = "Blunt";
    private static EntProtoId ActionFleshmend = "ActionFleshmend";
    private static EntProtoId Fleshmend = "StatusEffectFleshmend";
    private static EntProtoId Urist = "MobHuman";

    [SidedDependency(Side.Server)] private AntagVerbSystem _smite = default!;
    [SidedDependency(Side.Server)] private DamageableSystem _damage = default!;
    [SidedDependency(Side.Server)] private SharedActionsSystem _actions = default!;
    [SidedDependency(Side.Server)] private StatusEffectsSystem _status = default!;

    /// <summary>
    /// Verifies that a changeling will heal damage after using fleshmend.
    /// </summary>
    [Test]
    public async Task FleshmendHealsDamage()
    {
        var map = await Pair.CreateTestMap();
        var player = ServerSession!;
        var mob = EntityUid.Invalid;
        await Server.WaitAssertion(() =>
        {
            mob = SEntMan.SpawnEntity(Urist, map.GridCoords);
            SEntMan.RemoveComponent<BarotraumaComponent>(mob); // dont want them to interfere with healing
            SEntMan.RemoveComponent<RespiratorComponent>(mob);
            Server.PlayerMan.SetAttachedEntity(player, mob);

            _smite.MakeAntag(player, Smite);
            Assert.That(SEntMan.HasComponent<ChangelingIdentityComponent>(mob), $"Changeling antag smite didn't work on {SEntMan.ToPrettyString(mob)}");

            // do some damage that has to be healed
            var damage = new DamageSpecifier()
            {
                DamageDict = new()
                {
                    {Blunt, 25}
                }
            };
            _damage.ChangeDamage(mob, damage, targetPart: TargetBodyPart.Chest);
            Assert.That(_damage.GetTotalDamage(mob) == 25, "Damage should have been dealt");
        });

        // to make sure it doesnt auto heal later and have a false negative
        await RunSeconds(2);
        var total = _damage.GetTotalDamage(mob).Float();
        Assert.That(total, Is.GreaterThan(24), "Damage should not have healed itself");
        Assert.That(total, Is.LessThan(25.2), "Damage should not have randomly increased");

        await Server.WaitAssertion(() =>
        {
            // not using listing shit because it would probable be awful and breakable by starting points / fleshmend cost balancing
            if (_actions.AddAction(mob, ActionFleshmend) is not { } action)
            {
                Assert.Fail("Failed to add {ActionFleshmend} to {mob}");
                return;
            }

            var args = new RequestPerformActionEvent(SEntMan.GetNetEntity(action));
            Assert.That(_actions.TryPerformAction(mob, args), "Failed to use fleshmend action");

            Assert.That(_status.HasStatusEffect(mob, Fleshmend), "Fleshmend action did not add the status effect");
        });

        await RunSeconds(10); // should fully heal a small injury in this time...

        total = _damage.GetTotalDamage(mob).Float();
        Assert.That(total, Is.LessThan(5), "Fleshmend should have healed the ling");
    }
}
