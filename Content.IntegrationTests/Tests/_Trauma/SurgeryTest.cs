// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.IntegrationTests.Tests.Interaction;
using Content.Medical.Common.Targeting;
using Content.Medical.Common.Traumas;
using Content.Medical.Shared.Surgery;
using Content.Medical.Shared.Targeting;
using Content.Medical.Shared.Traumas;
using Content.Medical.Shared.Wounds;
using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Shared.Body;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Standing;
using Content.Shared.Temperature.Components;
using Content.Shared.Weapons.Melee;

namespace Content.IntegrationTests.Tests._Trauma;

public sealed class SurgeryTest : InteractionTest
{
    [SidedDependency(Side.Server)] private BodySystem _body = default!;
    [SidedDependency(Side.Server)] private DamageableSystem _damage = default!;
    [SidedDependency(Side.Server)] private SharedCombatModeSystem _combat = default!;
    [SidedDependency(Side.Server)] private SharedMeleeWeaponSystem _melee = default!;
    //[SidedDependency(Side.Server)] private SharedSurgerySystem _surgery = default!;
    [SidedDependency(Side.Server)] private SharedTargetingSystem _targeting = default!;
    [SidedDependency(Side.Server)] private StandingStateSystem _standing = default!;
    [SidedDependency(Side.Server)] private TraumaSystem _trauma = default!;
    [SidedDependency(Side.Server)] private WoundSystem _wound = default!;

    private static readonly EntProtoId Human = "MobHuman";
    private static readonly EntProtoId Weapon = "CaptainSabre";
    private static readonly ProtoId<OrganCategoryPrototype> ArmRight = "ArmRight";
    private static readonly ProtoId<OrganCategoryPrototype> Head = "Head";
    private static readonly ProtoId<OrganCategoryPrototype> Torso = "Torso";

    protected override string PlayerPrototype => Human;

    /// <summary>
    /// Checks that a sword can cut an arm off, leaving a dismemberment trauma on the torso.
    /// The trauma then has to be removed by surgery, which must allow reattaching the arm.
    /// The arm then is then tended which has to heal all its wounds.
    /// </summary>
    [Test]
    public async Task DismemberingTest()
    {
        var subject = await SpawnHuman();
        await Server.WaitAssertion(() =>
        {
            if (_body.GetOrgan(subject, Torso) is not { } torso)
            {
                Assert.Fail($"Urist had no torso!");
                return;
            }

            if (_body.GetOrgan(subject, ArmRight) is not { } arm)
            {
                Assert.Fail($"Urist had no right arm!");
                return;
            }

            // shouldn't start dismembered
            Assert.That(!_trauma.HasWoundableTrauma(torso, TraumaType.Dismemberment));
            Assert.That(_body.GetBody(arm), Is.EqualTo(subject));

            // have to lay down to guarantee it hits the limb instead of torso
            _standing.Down(subject);
            _combat.SetInCombatMode(SPlayer, true);
            _targeting.SetTarget(SPlayer, TargetBodyPart.RightArm);

            // try cutting arm with a sword until it falls off
            var weapon = SSpawn(Weapon, SEntMan.GetCoordinates(PlayerCoords));
            var melee = SComp<MeleeWeaponComponent>(weapon);
            for (var i = 0; i < 20; i++)
            {
                if (_body.GetBody(arm) == null)
                    break; // done

                melee.NextAttack = TimeSpan.Zero;
                _melee.AttemptLightAttack(SPlayer, weapon, melee, subject, canParry: false);
            }

            Assert.That(_body.GetBody(arm), Is.Null, $"{SEntMan.ToPrettyString(weapon)} failed to sever {SEntMan.ToPrettyString(arm)} in 20 hits!");

            Assert.That(_trauma.HasWoundableTrauma(torso, TraumaType.Dismemberment),
                "Arm was cut off but there was no dismemberment trauma left!");

            // TODO: do dismember surgery

            /*
            Assert.That(!_trauma.HasWoundableTrauma(torso, TraumaType.Dismemberment),
                "Surgery was finished but dismemberment trauma remained!");
            */

            // TODO: do reattach surgery

            // TODO: do tend brute surgery
        });
    }

    [Test]
    public async Task HealWoundsTest()
    {
        var subject = await SpawnHuman();
        await Server.WaitAssertion(() =>
        {
            if (_body.GetOrgan(subject, Head) is not { } head)
            {
                Assert.Fail("Urist has no head");
                return;
            }

            var amount = FixedPoint2.New(20);
            var damage = new DamageSpecifier()
            {
                DamageDict = new()
                {
                    { "Heat", amount }
                }
            };

            var part = TargetBodyPart.Head;
            _damage.ChangeDamage(subject, damage, targetPart: part, canMiss: false);
            Assert.That(_damage.GetTotalDamage(subject), Is.EqualTo(amount), $"Failed to damage the urist: {_damage.DumpDamage(subject)}");

            var wounds = _wound.GetWoundableWounds(head);
            Assert.That(wounds.Count, Is.EqualTo(1), "Expected only 1 wound");
            var wound = wounds[0];
            Assert.That(wound.Comp.WoundSeverityPoint, Is.EqualTo(amount), "Wound had wrong severity");

            // regular healing sources must heal the wound
            _damage.ChangeDamage(subject, -damage, targetPart: part, canMiss: false);
            Assert.That(_damage.GetTotalDamage(subject), Is.EqualTo(FixedPoint2.Zero), $"Failed to heal the urist: {_damage.DumpDamage(subject)}");
            AssertHealed(wound);

            _damage.ChangeDamage(subject, damage, targetPart: part, canMiss: false);
            Assert.That(_damage.GetTotalDamage(subject), Is.EqualTo(amount), $"Failed to damage the urist again: {_damage.DumpDamage(subject)}");

            wounds = _wound.GetWoundableWounds(head);
            Assert.That(wounds.Count, Is.EqualTo(1), "Expected only 1 wound");
            wound = wounds[0];
            Assert.That(wound.Comp.WoundSeverityPoint, Is.EqualTo(amount), "Wound had wrong severity");

            // direct wound healing must heal the wound
            Assert.That(_wound.TryHealWoundsOnOwner(subject, damage), "It should have healed the wound");
            AssertHealed(wound);

            wounds = _wound.GetWoundableWounds(head);
            Assert.That(wounds, Is.Empty, "Expected no leftover wounds");
            Assert.That(!_wound.TryHealWoundsOnOwner(subject, damage), "There should be no wounds left to heal");

            SDel(subject);
        });
    }

    private void AssertHealed(Entity<WoundComponent> wound)
    {
        Assert.That(wound.Comp.WoundSeverityPoint, Is.EqualTo(FixedPoint2.Zero), "Wound was not healed");
        Assert.That(SDeleted(wound), "Wound did not get deleted after being healed");
    }

    private async Task<EntityUid> SpawnHuman()
    {
        var mob = SEntMan.GetEntity(await SpawnTarget(Human));
        await Server.WaitPost(() =>
        {
            // dont want them to interfere with healing
            SRemComp<BarotraumaComponent>(mob);
            SRemComp<RespiratorComponent>(mob);
            SRemComp<TemperatureDamageComponent>(mob);
        });
        return mob;
    }
}
