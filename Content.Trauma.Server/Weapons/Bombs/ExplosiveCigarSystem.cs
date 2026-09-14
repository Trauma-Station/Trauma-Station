using Content.Medical.Common.Body;
using Content.Medical.Shared.Body;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Body;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Nutrition.Components;
using Content.Shared.Smoking;
using Content.Trauma.Shared.Weapons.Bombs.Components;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Weapons.Bombs;

public sealed partial class ExplosiveCigarSystem : EntitySystem
{
    [Dependency] private ExplosionSystem _explosion = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private BodyPartSystem _bodyPart = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ExplosiveCigarComponent, SmokableComponent>();
        while (query.MoveNext(out var uid, out var explosive, out var smokable))
        {
            if (explosive.TriggerAtRemaining == null)
                continue;

            if (smokable.State != SmokableState.Lit)
                continue;

            if (_timing.CurTime < explosive.NextCheck)
                continue;

            explosive.NextCheck = _timing.CurTime + explosive.CheckInterval;

            if (!_solutions.TryGetSolution(uid, smokable.Solution, out _, out var solution))
                continue;

            if (solution.Volume <= FixedPoint2.New(explosive.TriggerAtRemaining.Value))
                Explode(uid);
        }
    }

    [SubscribeLocalEvent]
    private void OnEmpty(Entity<ExplosiveCigarComponent> ent, ref SmokableSolutionEmptyEvent args)
    {
        if (ent.Comp.TriggerAtRemaining == null)
            Explode(ent);
    }

    private void Explode(EntityUid uid)
    {
        if (TryComp<ExplosiveCigarComponent>(uid, out var comp))
            TryApplyHeadDamage(uid, comp);

        _explosion.TriggerExplosive(uid);
        QueueDel(uid);
    }

    private void TryApplyHeadDamage(EntityUid uid, ExplosiveCigarComponent comp)
    {
        if (comp.MaskSlotHeadDamage == null)
            return;

        if (!_inventory.TryGetContainingSlot((uid, null, null), out var slot) || slot.Name != "mask")
            return;

        EntityUid wearer;
        if (_containers.TryGetContainingContainer(uid, out var container))
            wearer = container.Owner;
        else
            wearer = Transform(uid).ParentUid;

        if (!TryComp<BodyComponent>(wearer, out var body))
            return;

        var head = _bodyPart.FindBodyPart((wearer, body), BodyPartType.Head);
        if (head == null)
            return;

        _damageable.TryChangeDamage(head.Value.Owner, comp.MaskSlotHeadDamage);
    }
}
