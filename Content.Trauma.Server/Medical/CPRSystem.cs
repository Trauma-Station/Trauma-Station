// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Trauma.Shared.Medical;

namespace Content.Trauma.Server.Medical;

public sealed class CPRSystem : SharedCPRSystem
{
    [Dependency] private readonly RespiratorSystem _respirator = default!;

    private EntityQuery<RespiratorComponent> _respiratorQuery;

    public override void Initialize()
    {
        base.Initialize();

        _respiratorQuery = GetEntityQuery<RespiratorComponent>();
    }

    protected override void TryInhale(EntityUid uid)
    {
        if (!_respiratorQuery.TryComp(uid, out var comp))
            return;

        _respirator.Inhale((uid, comp));
        _respirator.Exhale((uid, comp)); // flush leftover gas to avoid gigadeath
    }
}
