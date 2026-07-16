// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Nuclear.Monitor;
using Content.Trauma.Shared.Nuclear.Turbine;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Nuclear.Monitor;

public sealed partial class GasTurbineMonitorSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private NuclearMonitorSystem _monitor = default!;
    [Dependency] private SharedTurbineSystem _turbine = default!;
    [Dependency] private EntityQuery<TurbineComponent> _query = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NuclearMonitorComponent, TurbineChangeFlowRateMessage>(_monitor.RelayMessage);
        SubscribeLocalEvent<NuclearMonitorComponent, TurbineChangeStatorLoadMessage>(_monitor.RelayMessage);
    }

    public Entity<TurbineComponent>? GetTurbine(EntityUid monitor)
        => _monitor.GetLinked(monitor) is { } uid && _query.TryComp(uid, out var comp)
            ? (uid, comp)
            : null;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<GasTurbineMonitorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextUpdate)
                continue;

            comp.NextUpdate = _timing.CurTime + comp.UpdateDelay;

            _monitor.CheckRange(uid);
        }
    }
}
