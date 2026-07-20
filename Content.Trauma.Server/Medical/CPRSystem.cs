// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Trauma.Shared.Medical;

namespace Content.Trauma.Server.Medical;

public sealed partial class CPRSystem : SharedCPRSystem
{
    [Dependency] private RespiratorSystem _respirator = default!;
    [Dependency] private EntityQuery<RespiratorComponent> _respiratorQuery = default!;

    protected override void TryInhale(EntityUid uid)
    {
        if (!_respiratorQuery.TryComp(uid, out var comp))
            return;

        _respirator.Inhale((uid, comp));
        _respirator.Exhale((uid, comp)); // flush leftover gas to avoid gigadeath
    }
}
