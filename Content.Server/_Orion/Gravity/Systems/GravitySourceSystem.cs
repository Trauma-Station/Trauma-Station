using Content.Server._Orion.Gravity.Components;
using Content.Server.Gravity;
using Content.Server.Power.Components;
using Content.Shared.Gravity;

namespace Content.Server._Orion.Gravity.Systems;

/// <summary>
///     System that... uhh... provides gravity by every ent with component without useless shit
/// </summary>
public sealed class GravitySourceSystem : EntitySystem
{
    [Dependency] private readonly GravitySystem _gravity = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GravitySourceComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<GravitySourceComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GravitySourceComponent, ApcComponent, PowerNetworkBatteryComponent, TransformComponent>();
        while (query.MoveNext(out _, out var gravitySource, out var apc, out var battery, out var xform))
        {
            var shouldBeActive = ShouldProvideGravity(apc, battery);

            if (gravitySource.Active == shouldBeActive)
                continue;

            gravitySource.Active = shouldBeActive;
            RefreshParentGravity(xform.ParentUid, shouldBeActive);
        }
    }

    private static bool ShouldProvideGravity(ApcComponent apc, PowerNetworkBatteryComponent battery)
    {
        if (!apc.MainBreakerEnabled)
            return false;

        var networkBattery = battery.NetworkBattery;
        return networkBattery.CurrentStorage > 0f || networkBattery.CurrentReceiving > 0f;
    }

    private void OnParentChanged(Entity<GravitySourceComponent> ent, ref EntParentChangedMessage args)
    {
        if (!ent.Comp.Active)
            return;

        if (args.OldParent is { } oldParent)
            RefreshParentGravity(oldParent, false);

        RefreshParentGravity(args.Transform.ParentUid, true);
    }

    private void OnShutdown(Entity<GravitySourceComponent> ent, ref ComponentShutdown args)
    {
        if (!ent.Comp.Active)
            return;

        var xform = Transform(ent);
        RefreshParentGravity(xform.ParentUid, false);
    }

    private void RefreshParentGravity(EntityUid parentUid, bool enable)
    {
        if (!HasComp<GravityComponent>(parentUid))
            return;

        if (enable)
            _gravity.EnableGravity(parentUid);
        else
            _gravity.RefreshGravity(parentUid);
    }
}
