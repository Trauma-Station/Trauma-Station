// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.DeviceLinking;

public sealed partial class SignalClockSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedDeviceLinkSystem _device = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var state = (_timing.CurTick.Value & 1) == 1;
        var query = EntityQueryEnumerator<SignalClockComponent>();
        foreach (var ent in query)
        {
            _device.SendSignal(ent.Owner, ent.Comp.Port, state);
        }
    }
}
