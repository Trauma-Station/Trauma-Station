// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Clothing.Components;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Trauma.Shared.Heretic.Components.Side;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Heretic.Systems.Side;

public sealed partial class MadnessMaskSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedHereticSystem _heretic = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedFearSystem _fear = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    private readonly HashSet<Entity<MobStateComponent>> _targets = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MadnessMaskComponent, BeingUnequippedAttemptEvent>(OnUnequip);
    }

    private void OnUnequip(Entity<MadnessMaskComponent> ent, ref BeingUnequippedAttemptEvent args)
    {
        var user = args.User;
        if (_heretic.IsHereticOrGhoul(user))
            return;

        if (TryComp<ClothingComponent>(ent, out var clothing) && (clothing.Slots & args.SlotFlags) == SlotFlags.NONE)
            return;

        args.Cancel();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<MadnessMaskComponent, ClothingComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var mask, out var clothing, out var xform))
        {
            if (clothing.InSlotFlag is null or SlotFlags.POCKET || now < mask.NextUpdate)
                continue;

            mask.NextUpdate = now + mask.UpdateDelay;

            var parent = xform.ParentUid;

            if (!_mobState.IsAlive(parent))
                continue;

            var coords = _transform.GetMapCoordinates(uid, xform);

            _targets.Clear();
            _lookup.GetEntitiesInRange(coords, mask.MaxRange, _targets, LookupFlags.Dynamic);
            foreach (var (target, mob) in _targets)
            {
                if (target == parent)
                    continue;

                if (!_mobState.IsAlive(target, mob))
                    continue;

                if (_heretic.IsHereticOrGhoul(target))
                    continue;

                if (!_container.IsInSameOrParentContainer(target, parent))
                    continue;

                var targetXform = Transform(target);
                var targetCoords = _transform.GetMapCoordinates(target, targetXform);

                if (!_examine.InRangeUnOccluded(targetCoords, coords, mask.MaxRange, null, entMan: EntityManager))
                    continue;

                var vec = coords.Position - targetCoords.Position;
                var len = vec.Length();
                if (len < 1e-4)
                {
                    _fear.AdjustFear(target, parent, mask.MaxFear);
                    continue;
                }

                var dir = _transform.GetWorldRotation(targetXform).ToWorldVec();

                var view = mask.ViewFearModifier * Vector2.Dot(dir, vec / len);
                var dist = mask.DistFearModifier * (1f - len / mask.MaxRange);

                var fear = view + dist;
                if (fear <= 0)
                    continue;

                _fear.AdjustFear(target, parent, MathF.Min(fear, mask.MaxFear));
            }
        }
    }
}
