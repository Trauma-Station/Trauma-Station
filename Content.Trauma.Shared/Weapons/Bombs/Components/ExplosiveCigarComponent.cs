// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Weapons.Bombs.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ExplosiveCigarComponent : Component
{
    /// <summary>
    /// How much solution must remain before it explodes.
    /// If null, explodes when the solution is fully empty.
    /// </summary>
    [DataField]
    public float? TriggerAtRemaining;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextCheck = TimeSpan.Zero;

    [DataField]
    public TimeSpan CheckInterval = TimeSpan.FromSeconds(3);
}
