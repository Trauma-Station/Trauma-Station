// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Whitelist;
using Content.Trauma.Shared.Revolutionary;

namespace Content.IntegrationTests.Tests._Trauma;

/// <summary>
/// Makes sure revolutionary conversion works.
/// </summary>
[Category("GameRuleTests")]
public sealed class RevsTest : InteractionTest
{
    public static readonly EntProtoId Urist = "MobHuman";
    public static readonly EntProtoId Mouse = "MobMouse";
    public static readonly EntProtoId Propaganda = "RevPropaganda";
    public static readonly EntProtoId MindShieldImplant = "MindShieldImplant";
    public static readonly EntProtoId MindShieldImplanter = "MindShieldImplanter";
    public static readonly EntProtoId DefaultRevsRule = "Revolutionary";
    public static readonly ProtoId<RadioChannelPrototype> HeadRevRadio = "HeadRevolutionary";

    protected override string PlayerPrototype => Urist; // needs to have a tongue to speak

    [SidedDependency(Side.Server)] private ActionBlockerSystem _blocker = default!;
    [SidedDependency(Side.Server)] private AntagSelectionSystem _antag = default!;
    [SidedDependency(Side.Server)] private EntityWhitelistSystem _whitelist = default!;
    [SidedDependency(Side.Server)] private RevPropagandaSystem _rev = default!;
    [SidedDependency(Side.Server)] private SharedMindSystem _mind = default!;
    [SidedDependency(Side.Server)] private SharedRoleSystem _roles = default!;
    [SidedDependency(Side.Server)] private SharedSubdermalImplantSystem _implant = default!;

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

    /// <summary>
    /// Makes sure that headrevs start with a radio implant and can use the headrev radio channel.
    /// </summary>
    [Test]
    public async Task HeadrevHasRadio()
    {
        Assert.That(!SHasComp<ImplantedComponent>(SPlayer), "Urist shouldnt be implanted");
        await MakePlayerHeadRev();
        Assert.That(SHasComp<ImplantedComponent>(SPlayer), "Headrev should have gotten a radio implant");
        var radio = SComp<ActiveRadioComponent>(SPlayer);
        Assert.That(radio.Channels.Contains(HeadRevRadio), "Radio implant did not add the headrev channel");
    }

    /// <summary>
    /// Makes sure headrevs break mindshields and can't convert while mindshielded.
    /// </summary>
    [Test]
    public async Task HeadrevBreaksMindshield()
    {
        var implant = EntityUid.Invalid;
        await MakePlayerHeadRev();
        await Server.WaitPost(() =>
        {
            implant = _implant.AddImplant(SPlayer, MindShieldImplant)!.Value;
        });
        Assert.That(!SComp<HeadRevolutionaryComponent>(SPlayer).ConvertAbilityEnabled, "Mind shield didn't disable conversion");
        Assert.That(STryComp<MindShieldStatusComponent>(SPlayer, out var shield), "Mind shield didn't get broken");
        Assert.That(shield.IsBroken, "Mind shield was not broken on headrev");

        await SpawnTarget(Urist);
        await AddTargetMind();
        await AssertConvert("Mindshielded headrevs must not be able to convert players");
        await DelTarget();

        await Server.WaitPost(() =>
        {
            _implant.ForceRemove(SPlayer, implant);
        });
        Assert.That(SComp<HeadRevolutionaryComponent>(SPlayer).ConvertAbilityEnabled, "Removing mind shield didn't re-enable conversion");
    }

    private async Task AssertConvert(string reason, bool works = false)
    {
        var netPropaganda = await PlaceInHands(Propaganda);
        var propaganda = SEntMan.GetEntity(netPropaganda);
        var comp = SComp<RevPropagandaComponent>(propaganda);
        var user = SPlayer;
        var target = STarget!.Value;
        if (works)
        {
            // individual checks are easier to understand than blanket "no it dont work"
            Assert.That(SComp<HeadRevolutionaryComponent>(user).ConvertAbilityEnabled, "Headrev must not be mindshielded");
            Assert.That(SComp<MindContainerComponent>(target).HasMind, "Target player must have a mind");
            Assert.That(_blocker.CanSpeak(user), "Head rev must be able to speak");
            Assert.That(_whitelist.IsWhitelistFailOrNull(comp.UserBlacklist, user), $"User blacklist passed for {SEntMan.ToPrettyString(user)}");
            Assert.That(_whitelist.IsWhitelistPassOrNull(comp.UserWhitelist, user), $"User whitelist failed for {SEntMan.ToPrettyString(user)}");
            Assert.That(_whitelist.IsWhitelistFailOrNull(comp.Blacklist, target), $"Target blacklist passed for {SEntMan.ToPrettyString(target)}");
            Assert.That(_whitelist.IsWhitelistPassOrNull(comp.Whitelist, target), $"Target whitelist failed for {SEntMan.ToPrettyString(target)}");
        }
        Assert.That(_rev.CanConvert((propaganda, comp), user, target), Is.EqualTo(works), $"Wrong CanConvert result for {reason}");
        await Interact();

        var converted = IsTargetRev();
        Assert.That(converted, Is.EqualTo(works), reason);
        if (works)
        {
            // conversion count must've gone up too
            var mind = SComp<MindContainerComponent>(SPlayer).Mind;
            Assert.That(mind != null, "Head rev must have a mind");
            Assert.That(_roles.MindHasRole<RevolutionaryRoleComponent>(mind!.Value, out var role), "Head rev must have the role");
            Assert.That(role.Value.Comp2.ConvertedCount > 0, "ConvertedCount must go up after a conversion");
        }
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
            _antag.ForceMakeAntag<RevolutionaryRuleComponent>(ServerSession, DefaultRevsRule);
            Assert.That(SHasComp<HeadRevolutionaryComponent>(SPlayer), "Making test player a headrev failed");
            Assert.That(SComp<MindContainerComponent>(SPlayer).HasMind, "Test's player must have a mind");
        });
    }

    private async Task AddTargetMind()
    {
        await Server.WaitPost(() =>
        {
            var target = STarget!.Value;
            var mind = _mind.CreateMind(null, "Test Player");
            _mind.TransferTo(mind, target, mind: mind.Comp);
            Assert.That(SComp<MindContainerComponent>(target).HasMind, "Target mob did not have a mind after transferring one into it");
        });
    }

    private async Task DelTarget()
    {
        await Delete(STarget!.Value);
    }
}
