// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Shared.Pain;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Medical.Shared.Consciousness;

public sealed partial class ConsciousnessSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PainSystem _pain = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitProcess();
        InitNet();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdatePassedOut(frameTime);
    }
}
