using Content.Trauma.Common.Nutrition;
using Content.Shared.Mobs;
using Content.Shared.Nutrition;
using Content.Shared.Sprite;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Systems;
using System.Numerics;

namespace Content.Goobstation.Shared.EatToGrow;

public sealed class EatToGrowSystem : EntitySystem
{
    [Dependency] private readonly SharedScaleVisualsSystem _scale = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EatToGrowComponent, FullyAteEvent>(OnFoodEaten);
        SubscribeLocalEvent<EatToGrowComponent, MobStateChangedEvent>(ShrinkOnDeath);
    }

    private void OnFoodEaten(Entity<EatToGrowComponent> ent, ref FullyAteEvent args)
    {
        // if growing would go over the limit, return
        if (ent.Comp.CurrentScale >= ent.Comp.MaxGrowth)
            return;

        Grow(ent);
    }

    private void Grow(Entity<EatToGrowComponent> ent)
    {
        // Uses scale variable to multiply the growth, mainly used for shrinking
        // Add growth
        ent.Comp.CurrentScale += ent.Comp.Growth;
        ent.Comp.CurrentScale = MathF.Min(ent.Comp.CurrentScale, ent.Comp.MaxGrowth);
        ent.Comp.TimesGrown++;
        Dirty(ent);

        ChangeScale(ent, ent.Comp.Growth);

        // Grow the fixture by 1/4 the growth
        ChangeFixtures(ent, ent.Comp.Growth / 4);
    }

    private void ShrinkOnDeath(Entity<EatToGrowComponent> ent, ref MobStateChangedEvent args)
    {
        if (!ent.Comp.ShrinkOnDeath || ent.Comp.TimesGrown == 0 || args.NewMobState != MobState.Dead)
            return;

        // shrink the entity
        ChangeFixtures(ent, ent.Comp.TimesGrown * -ent.Comp.Growth / 4);
        ChangeScale(ent, 1f - ent.Comp.CurrentScale);
        ent.Comp.CurrentScale = 1f;
        ent.Comp.TimesGrown = 0;
        Dirty(ent);
    }

    private void ChangeScale(EntityUid uid, float add)
    {
        var old = _scale.GetSpriteScale(uid);
        _scale.SetSpriteScale(uid, old + new Vector2(add, add));
    }

    private void ChangeFixtures(EntityUid uid, float add)
    {
        if (!TryComp<FixturesComponent>(uid, out var fixtures))
            return;

        foreach (var (id, fixture) in fixtures.Fixtures)
        {
            if (fixture.Shape is not PhysShapeCircle circle)
                continue;

            _physics.SetPositionRadius(
                uid, id, fixture, circle,
                circle.Position, circle.Radius + add, fixtures);
        }
    }
}
