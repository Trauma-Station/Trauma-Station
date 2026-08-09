// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Actions;
using Content.Trauma.Server.Antag;
using Content.Trauma.Shared.Antag;
using Content.Trauma.Shared.CosmicCult.Components;

namespace Content.IntegrationTests.Tests._Trauma;

public sealed class CosmicCultTest : InteractionTest
{
    private static readonly EntProtoId Urist = "MobHuman";
    private static readonly ProtoId<AntagSmitePrototype> Smite = "CosmicCultist";

    protected override string PlayerPrototype => Urist;

    [SidedDependency(Side.Server)] private AntagVerbSystem _smite = default!;
    [SidedDependency(Side.Server)] private SharedActionsSystem _actions = default!;

    /// <summary>
    /// Checks that levelling up the cult works.
    /// Can't check the UI from a player's perspective so it's not foolproof.
    /// </summary>
    [Test]
    public async Task CultLevelUpTest()
    {
        await Server.WaitPost(() =>
        {
            _smite.MakeAntag(ClientSession, Smite);
        });

        var cultist = SComp<CosmicCultComponent>(SPlayer);
        Assert.That(cultist.ShopActionEntity, Is.Not.Null);

        var start = cultist.CurrentLevel;

        _actions.SetUseDelay(cultist.SiphonActionEntity, null); // need to spam it
        while (cultist.EntropyBudget < cultist.EntropyForNextLevel)
        {
            await Siphon(cultist);
        }

        await Server.WaitAssertion(() =>
        {
            var shop = SEntMan.GetNetEntity(cultist.ShopActionEntity);

            Assert.That(cultist.LevelUpAwaitingConfirmation, "Should be able to level up after siphoning enough entropy");

            var args = new RequestPerformActionEvent(shop.Value);
            Assert.That(_actions.TryPerformAction(SPlayer, args), "Failed to use shop action");
            var msg = new LevelUpconfirmedMessage()
            {
                Actor = SPlayer
            };
            SEntMan.EventBus.RaiseLocalEvent(cultist.ShopActionEntity!.Value, msg);

            Assert.That(!cultist.LevelUpAwaitingConfirmation, "Level up message should have levelled up");
            Assert.That(cultist.CurrentLevel, Is.GreaterThan(start), "Level up did not increase cultist level");
        });
    }

    private async Task Siphon(CosmicCultComponent cultist)
    {
        var start = cultist.EntropyBudget;

        Assert.That(cultist.SiphonActionEntity, Is.Not.Null);

        var action = SEntMan.GetNetEntity(cultist.SiphonActionEntity);
        Target ??= await SpawnTarget(Urist);

        await Server.WaitAssertion(() =>
        {
            var args = new RequestPerformActionEvent(action.Value, Target.Value);
            Assert.That(_actions.TryPerformAction(SPlayer, args), "Failed to use siphon entropy action");
        });
        await AwaitDoAfters();
        Assert.That(cultist.EntropyBudget, Is.GreaterThan(start), "Siphon action should've increased entropy");
    }
}
