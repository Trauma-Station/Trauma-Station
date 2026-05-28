using System.Numerics;
using System.Runtime.CompilerServices;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Damageable;

/* Trauma - takes 5m for a single prototype somehow + its not correct anyway. commenting out instead of Explicit so it doesnt have 6000 line log
[TestFixture]
[TestOf(typeof(DamageableComponent))]
[TestOf(typeof(DamageableSystem))]
public sealed class DamageAllPrototypesTest : GameTest
{
    [SidedDependency(Side.Server)] private readonly DamageableSystem _damageableSystem = default!;

    private static string[] _damageables = GameDataScrounger.EntitiesWithComponent("Damageable");

    [Test]
    [TestOf(typeof(DamageableSystem))]
    [TestCaseSource(nameof(_damageables))]
    [Description("Ensures all Entity Prototypes with damageable can be damaged.")]
    public async Task TestDamageableComponents(string damageable)
    {
        var map = await Pair.CreateTestMap();

        var entity = await SpawnAtPosition(damageable, map.GridCoords);

        // Intentionally cannot take damage, ignore it.
        if (SEntMan.HasComponent<GodmodeComponent>(entity))
            return;

        var canBeDamaged = false;

        foreach (var type in SProtoMan.EnumeratePrototypes<DamageTypePrototype>())
        {
            if (!_damageableSystem.CanBeDamagedBy(entity, type))
                continue;

            canBeDamaged = true;

            await Server.WaitPost(() =>
            {
                var damage = new DamageSpecifier(type, 1); // Trauma - use 1, Epsilon can be lost from rounding
                var previousDamage = _damageableSystem.GetTotalDamage(entity);
                _damageableSystem.ChangeDamage(entity, damage, ignoreResistances: true);
                Assert.That(_damageableSystem.GetTotalDamage(entity) > previousDamage, $"{SEntMan.ToPrettyString(entity)} failed to take {type.ID} damage!"); // Trauma - check > not ==, add message
                _damageableSystem.ClearAllDamage(entity);
            });
        }

        // Ensure that this entity can actually be damaged.
        Assert.That(canBeDamaged);
    }
}
*/
