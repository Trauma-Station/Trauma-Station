// <Trauma>
using Content.Shared.Alert;
using Content.Shared.Whitelist;
// </Trauma>
using Content.Shared.Interaction;
using Content.Shared.Pinpointer;
using System.Linq;
using System.Numerics;
using Robust.Shared.Utility;
using Content.Server.Shuttles.Events;

namespace Content.Server.Pinpointer;

public sealed class PinpointerSystem : SharedPinpointerSystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!; // WD EDIT
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();
        _xformQuery = GetEntityQuery<TransformComponent>();

        // WD EDIT START
        SubscribeLocalEvent<PinpointerComponent, MapInitEvent>(OnMapInit); // TODO: move to a different system bruh
        SubscribeLocalEvent<PinpointerComponent, ComponentShutdown>(OnShutdown);
        // WD EDIT END
        SubscribeLocalEvent<PinpointerComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<FTLCompletedEvent>(OnLocateTarget);
    }

    // WD EDIT START
    private void OnMapInit(EntityUid uid, PinpointerComponent component, MapInitEvent args)
    {
        if (component.Alert.HasValue)
            _alerts.ShowAlert(uid, component.Alert.Value);
    }

    private void OnShutdown(EntityUid uid, PinpointerComponent component, ComponentShutdown args)
    {
        if (component.Alert.HasValue)
            _alerts.ClearAlert(uid, component.Alert.Value);
    }
    // WD EDIT END

    public override bool TogglePinpointer(Entity<PinpointerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var isActive = !ent.Comp.IsActive;
        SetActive(ent, isActive);
        UpdateAppearance(ent);
        return isActive;
    }

    private void UpdateAppearance(Entity<PinpointerComponent?, AppearanceComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1) || !Resolve(ent, ref ent.Comp2))
            return;

        _appearance.SetData(ent, PinpointerVisuals.IsActive, ent.Comp1.IsActive, ent.Comp2);
        _appearance.SetData(ent, PinpointerVisuals.TargetDistance, ent.Comp1.DistanceToTarget, ent.Comp2);
    }

    private void OnActivate(Entity<PinpointerComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (ent.Comp.CanToggle) // WD EDIT
            TogglePinpointer(ent.AsNullable());

        if (!ent.Comp.CanRetarget)
            LocateTarget(ent);

        args.Handled = true;
    }

    private void OnLocateTarget(ref FTLCompletedEvent ev)
    {
        // This feels kind of expensive, but it only happens once per hyperspace jump

        // todo: ideally, you would need to raise this event only on jumped entities
        // this code update ALL pinpointers in game

        var query = EntityQueryEnumerator<PinpointerComponent>();
        while (query.MoveNext(out var uid, out var pinpointer))
        {
            if (pinpointer.CanRetarget)
                continue;

            // <Trauma>
            if (Transform(uid).GridUid != ev.Entity)
                continue;
            // </Trauma>

            LocateTarget((uid, pinpointer));
        }
    }

    /// <summary>
    /// Goob edit: this was literally fully changed. But still works as intended
    /// </summary>
    private void LocateTarget(Entity<PinpointerComponent> ent)
    {
        if (!ent.Comp.IsActive || ent.Comp.Whitelist == null)
            return;

        if (ent.Comp.CanTargetMultiple)
        {
            var targets = FindAllTargetsFromComponent(ent.Owner, ent.Comp.Whitelist, ent.Comp.Blacklist);
            SetTargets(ent.AsNullable(), targets);
        }
        else
        {
            var target = FindTargetFromComponent(ent.Owner, ent.Comp.Whitelist, ent.Comp.Blacklist);
            SetTarget(ent.AsNullable(), target);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // because target or pinpointer can move
        // we need to update pinpointers arrow each frame
        var query = EntityQueryEnumerator<PinpointerComponent>();
        while (query.MoveNext(out var uid, out var pinpointer))
        {
            UpdateDirectionToTarget((uid, pinpointer));
        }
    }

    /// <summary>
    ///     Try to find the closest entity from whitelist on a current map
    ///     Will return null if can't find anything
    ///     Goob edit: requires EntityWhitelist instead of just Type.
    /// </summary>
    private EntityUid? FindTargetFromComponent(Entity<TransformComponent?> ent, EntityWhitelist whitelist, EntityWhitelist? blacklist)
    {
        if (!Resolve(ent, ref ent.Comp))
            return null;

        var transform = ent.Comp;

        // sort all entities in distance increasing order
        var mapId = ent.Comp.MapID;
        var l = new SortedList<float, EntityUid>();
        var worldPos = _transform.GetWorldPosition(ent.Comp);

        // Goob edit start
        if (whitelist.Components == null)
            return null;

        foreach (var component in whitelist.Components)
        {
            if (!EntityManager.ComponentFactory.TryGetRegistration(component, out var reg))
            {
                Log.Error($"Unable to find component registration for {component} for pinpointer!");
                DebugTools.Assert(false);
                return null;
            }

            foreach (var (otherUid, _) in EntityManager.GetAllComponents(reg.Type))
            {
                if (!_xformQuery.TryGetComponent(otherUid, out var compXform) || compXform.MapID != mapId)
                    continue;

                if (Whitelist.IsWhitelistPass(blacklist, otherUid))
                    continue;

                var dist = (_transform.GetWorldPosition(compXform) - worldPos).LengthSquared();
                l.TryAdd(dist, otherUid);
            }
        }
        // Goob edit end

        // return uid with a smallest distance
        return l.Count > 0 ? l.First().Value : null;
    }

    /// <summary>
    /// Goob edit: Gets all possible targets within it's whitelist relative to pinpointer entity.
    /// </summary>
    private List<EntityUid> FindAllTargetsFromComponent(
        Entity<TransformComponent?> ent,
        EntityWhitelist whitelist,
        EntityWhitelist? blacklist)
    {
        _xformQuery.Resolve(ent, ref ent.Comp, false);
        var list = new List<EntityUid>();

        if (ent.Comp == null)
            return list;

        var transform = ent.Comp;
        var mapId = transform.MapID;

        if (whitelist.Components == null)
            return list;

        foreach (var component in whitelist.Components)
        {
            if (!EntityManager.ComponentFactory.TryGetRegistration(component, out var reg))
            {
                Log.Error($"Unable to find component registration for {component} for pinpointer!");
                DebugTools.Assert(false);
                return list;
            }

            foreach (var (otherUid, _) in EntityManager.GetAllComponents(reg.Type))
            {
                if (!_xformQuery.TryGetComponent(otherUid, out var compXform) || compXform.MapID != mapId)
                    continue;

                if (Whitelist.IsWhitelistPass(blacklist, otherUid))
                    continue;

                list.Add(otherUid);
            }
        }

        return list;
    }

    /// <summary>
    ///     Update direction from pinpointer to selected target (if it was set)
    /// </summary>
    protected override void UpdateDirectionToTarget(Entity<PinpointerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var pinpointer = ent.Comp;

        if (!pinpointer.IsActive)
            return;

        var target = GetNearestTarget((ent, ent.Comp));
        if (target == null || !Exists(target.Value))
        {
            SetDistance(ent, Distance.Unknown);
            LocateTarget((ent, ent.Comp)); // Trauma
            return;
        }

        var dirVec = CalculateDirection(ent, target.Value);
        var oldDist = pinpointer.DistanceToTarget;
        if (dirVec != null)
        {
            var angle = dirVec.Value.ToWorldAngle();
            TrySetArrowAngle(ent, angle);
            var dist = CalculateDistance(dirVec.Value, pinpointer);
            SetDistance(ent, dist);
        }
        else
        {
            SetDistance(ent, Distance.Unknown);
        }
        if (oldDist != pinpointer.DistanceToTarget)
            UpdateAppearance(ent);
    }

    /// <summary>
    ///     Calculate direction from pinUid to trgUid
    /// </summary>
    /// <returns>Null if failed to calculate distance between two entities</returns>
    private Vector2? CalculateDirection(EntityUid pinUid, EntityUid trgUid)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();

        // check if entities have transform component
        if (!xformQuery.TryGetComponent(pinUid, out var pin))
            return null;
        if (!xformQuery.TryGetComponent(trgUid, out var trg))
            return null;

        // check if they are on same map
        if (pin.MapID != trg.MapID)
            return null;

        // get world direction vector
        var dir = _transform.GetWorldPosition(trg, xformQuery) - _transform.GetWorldPosition(pin, xformQuery);
        return dir;
    }

    /// <summary>
    /// Goob edit: gets the nearest target out of pinpointer's Targets list.
    /// </summary>
    private EntityUid? GetNearestTarget(Entity<PinpointerComponent> ent)
    {
        var list = new SortedList<float, EntityUid>();
        foreach (var target in ent.Comp.Targets)
        {
            var lengh = CalculateDirection(ent, target);
            if (lengh == null)
                continue;

            var dist = lengh.Value.Length();
            if (!list.TryAdd(dist, target))
                list.TryAdd(dist + 1f, target); // safety measure
        }

        return list.Count > 0 ? list.First().Value : null;
    }

    private Distance CalculateDistance(Vector2 vec, PinpointerComponent pinpointer)
    {
        var dist = vec.Length();
        if (dist <= pinpointer.ReachedDistance)
            return Distance.Reached;
        else if (dist <= pinpointer.CloseDistance)
            return Distance.Close;
        else if (dist <= pinpointer.MediumDistance)
            return Distance.Medium;
        else
            return Distance.Far;
    }
}
