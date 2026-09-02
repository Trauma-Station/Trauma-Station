// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Nuclear.Monitor;
using Content.Trauma.Shared.Nuclear.Reactor;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Nuclear.Monitor;

public sealed partial class NuclearReactorMonitorSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private NuclearMonitorSystem _monitor = default!;
    [Dependency] private SharedNuclearReactorSystem _reactor = default!;
    [Dependency] private EntityQuery<NuclearReactorComponent> _query = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NuclearMonitorComponent, ReactorAdjustControlRodsMessage>(_monitor.RelayMessage);
    }

    public Entity<NuclearReactorComponent>? GetReactor(EntityUid monitor)
        => _monitor.GetLinked(monitor) is { } uid && _query.TryComp(uid, out var comp)
            ? (uid, comp)
            : null;

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<NuclearReactorMonitorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextUpdate)
                continue;

            _monitor.CheckRange(uid);
            if (GetReactor(uid) is { } reactor)
                _reactor.UpdateUI(reactor);
        }
    }
}
