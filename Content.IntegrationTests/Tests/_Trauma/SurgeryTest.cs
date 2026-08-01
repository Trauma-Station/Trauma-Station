// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Medical.Common.Targeting;
using Content.Medical.Common.Traumas;
using Content.Medical.Shared.Surgery;
using Content.Medical.Shared.Targeting;
using Content.Medical.Shared.Traumas;
using Content.Shared.Body;
using Content.Shared.CombatMode;
using Content.Shared.Standing;
using Content.Shared.Weapons.Melee;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Trauma;

public sealed class SurgeryTest : GameTest
{
    public static readonly EntProtoId Human = "MobHuman";
    public static readonly EntProtoId Weapon = "CaptainSabre";
    public static readonly ProtoId<OrganCategoryPrototype> Torso = "Torso";
    public static readonly ProtoId<OrganCategoryPrototype> ArmRight = "ArmRight";

    [SidedDependency(Side.Server)] private BodySystem _body = default!;
    [SidedDependency(Side.Server)] private SharedCombatModeSystem _combat = default!;
    [SidedDependency(Side.Server)] private SharedMeleeWeaponSystem _melee = default!;
    //[SidedDependency(Side.Server)] private SharedSurgerySystem _surgery = default!;
    [SidedDependency(Side.Server)] private SharedTargetingSystem _targeting = default!;
    [SidedDependency(Side.Server)] private StandingStateSystem _standing = default!;
    [SidedDependency(Side.Server)] private TraumaSystem _trauma = default!;

    /// <summary>
    /// Checks that a sword can cut an arm off, leaving a dismemberment trauma on the torso.
    /// The trauma then has to be removed by surgery, which must allow reattaching the arm.
    /// The arm then is then tended which has to heal all its wounds.
    /// </summary>
    [Test]
    public async Task DismemberingTest()
    {
        var map = await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var subject = SEntMan.SpawnEntity(Human, map.GridCoords);
            var doctor = SEntMan.SpawnEntity(Human, map.GridCoords);

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
            _combat.SetInCombatMode(doctor, true);
            _targeting.SetTarget(doctor, TargetBodyPart.RightArm);

            // try cutting arm with a sword until it falls off
            var weapon = SEntMan.SpawnEntity(Weapon, map.GridCoords);
            var melee = SEntMan.GetComponent<MeleeWeaponComponent>(weapon);
            for (var i = 0; i < 20; i++)
            {
                if (_body.GetBody(arm) == null)
                    break; // done

                melee.NextAttack = TimeSpan.Zero;
                _melee.AttemptLightAttack(doctor, weapon, melee, subject, canParry: false);
            }

            Assert.That(_body.GetBody(arm), Is.Null, $"{SEntMan.ToPrettyString(weapon)} failed to sever {SEntMan.ToPrettyString(arm)} in 20 hits!");

            Assert.That(_trauma.HasWoundableTrauma(torso, TraumaType.Dismemberment),
                "Arm was cut off but there was no dismemberment trauma left!");

            // TODO: do dismember surgery

            /*
            Assert.That(_trauma.HasWoundableTrauma(torso, TraumaType.Dismemberment),
                "Surgery was finished but Arm was cut off but there was no dismemberment trauma left!");
            */

            // TODO: do reattach surgery

            // TODO: do tend brute surgery
        });
    }
}
