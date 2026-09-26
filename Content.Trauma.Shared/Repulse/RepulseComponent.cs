// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Repulse;

/// <summary>
/// Stuns you and sends you flying if you try to touch this entity.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(RepulseSystem))]
public sealed partial class RepulseComponent : Component
{
    [DataField]
    public float Impulse = 130;

    [DataField]
    public TimeSpan KnockdownDuration = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(3);
}

/// <summary>
/// Raised on a repulsor when trying to repulse an entity that touched it.
/// </summary>
[ByRefEvent]
public record struct RepulseAttemptEvent(EntityUid Target, bool Cancelled = false);
