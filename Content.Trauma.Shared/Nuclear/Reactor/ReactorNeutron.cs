// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Nuclear.Reactor;

/// <summary>
/// A virtual neutron that flies around within the reactor.
/// </summary>
[DataRecord, Serializable]
public sealed partial class ReactorNeutron
{
    public Direction Dir;
    public float Velocity;

    public ReactorNeutron()
    {
    }

    public ReactorNeutron(Direction dir, float velocity)
    {
        Dir = dir;
        Velocity = velocity;
    }
}
