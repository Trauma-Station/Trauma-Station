// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Server.Body.Systems;
using Content.Trauma.Shared.Medical;

namespace Content.Trauma.Server.Medical;

public sealed class CPRSystem : SharedCPRSystem
{
    [Dependency] private readonly RespiratorSystem _respirator = default!;

    protected override void TryInhale(EntityUid uid)
    {
        _respirator.Inhale(uid);
    }
}
