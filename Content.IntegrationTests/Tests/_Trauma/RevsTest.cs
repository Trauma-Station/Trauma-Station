// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Whitelist;
using Content.Trauma.Shared.Revolutionary;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Trauma;

/// <summary>
/// Makes sure revolutionary conversion works.
/// </summary>
[TestFixture]
public sealed class RevsTest : InteractionTest
{
    public static readonly EntProtoId Urist = "MobHuman";
    public static readonly EntProtoId Mouse = "MobMouse";
    public static readonly EntProtoId Propaganda = "RevPropaganda";
    public static readonly EntProtoId MindShieldImplanter = "MindShieldImplanter";
    public static readonly EntProtoId DefaultRevsRule = "Revolutionary";

    protected override string PlayerPrototype => Urist; // needs to have a tongue to speak

    /// <summary>
    /// Checks that using propaganda on:
    /// - a player as a non-rev fails
    /// - a mouse fails
    /// - a mindless non-rev fails
    /// - a non-rev with a mind succeeds
    /// - a mindshielded non-rev fails
    /// </summary>
    [Test]
    public async Task RevPropagandaWorks()
    {
        await SpawnTarget(Urist);
        await AddTargetMind();
        await AssertConvert("Non-revs must not be able to convert a player");
        await DelTarget();

        await MakePlayerHeadRev();

        await SpawnTarget(Mouse);
        await AssertConvert("Revs must not be able to convert a mouse");
        await DelTarget();

        await SpawnTarget(Urist);
        await AssertConvert("Revs must not be able to convert mindless people");
        await AddTargetMind();
        await AssertConvert("Revs must be able to convert players", works: true);
        await DelTarget();

        await SpawnTarget(Urist);
        await AddTargetMind();
        await InteractUsing(MindShieldImplanter);
        await AssertConvert("Revs must not be able to convert mindshielded people");
        await DelTarget();
    }

    private async Task AssertConvert(string reason, bool works = false)
    {
        var netPropaganda = await PlaceInHands(Propaganda);
        var propaganda = SEntMan.GetEntity(netPropaganda);
        var comp = SEntMan.GetComponent<RevPropagandaComponent>(propaganda);
        var rev = SEntMan.System<RevPropagandaSystem>();
        var user = SPlayer;
        var target = STarget!.Value;
        if (works)
        {
            var blocker = SEntMan.System<ActionBlockerSystem>();
            var whitelist = SEntMan.System<EntityWhitelistSystem>();
            // individual checks are easier to understand than blanket "no it dont work"
            Assert.That(SEntMan.GetComponent<HeadRevolutionaryComponent>(user).ConvertAbilityEnabled, "Headrev must not be mindshielded");
            Assert.That(SEntMan.GetComponent<MindContainerComponent>(target).HasMind, "Target player must have a mind");
            Assert.That(blocker.CanSpeak(user), "Head rev must be able to speak");
            Assert.That(whitelist.IsWhitelistFailOrNull(comp.UserBlacklist, user), $"User blacklist passed for {SEntMan.ToPrettyString(user)}");
            Assert.That(whitelist.IsWhitelistPassOrNull(comp.UserWhitelist, user), $"User whitelist failed for {SEntMan.ToPrettyString(user)}");
            Assert.That(whitelist.IsWhitelistFailOrNull(comp.Blacklist, target), $"Target blacklist passed for {SEntMan.ToPrettyString(target)}");
            Assert.That(whitelist.IsWhitelistPassOrNull(comp.Whitelist, target), $"Target whitelist failed for {SEntMan.ToPrettyString(target)}");
        }
        Assert.That(rev.CanConvert((propaganda, comp), user, target), Is.EqualTo(works), $"Wrong CanConvert result for {reason}");
        await Interact();

        var converted = IsTargetRev();
        Assert.That(converted, Is.EqualTo(works), reason);
    }

    private bool IsTargetRev()
    {
        var roleSys = SEntMan.System<SharedRoleSystem>();
        if (SEntMan.GetComponentOrNull<MindContainerComponent>(STarget)?.Mind is not { } mind)
            return false; // no mind to convert

        return roleSys.MindHasRole<RevolutionaryRoleComponent>(mind, out _);
    }

    private async Task MakePlayerHeadRev()
    {
        await Server.WaitPost(() =>
        {
            var antag = SEntMan.System<AntagSelectionSystem>();
            antag.ForceMakeAntag<RevolutionaryRuleComponent>(ServerSession, DefaultRevsRule);
            Assert.That(SEntMan.HasComponent<HeadRevolutionaryComponent>(SPlayer), "Making test player a headrev failed");
            Assert.That(SEntMan.GetComponent<MindContainerComponent>(SPlayer).HasMind, "Test's player must have a mind");
        });
    }

    private async Task AddTargetMind()
    {
        await Server.WaitPost(() =>
        {
            var target = STarget!.Value;
            var mindSys = SEntMan.System<SharedMindSystem>();
            var mind = mindSys.CreateMind(null, "Test Player");
            mindSys.TransferTo(mind, target, mind: mind.Comp);
            Assert.That(SEntMan.GetComponent<MindContainerComponent>(target).HasMind, "Target mob did not have a mind after transferring one into it");
        });
    }

    private async Task DelTarget()
    {
        await Delete(STarget!.Value);
    }
}
