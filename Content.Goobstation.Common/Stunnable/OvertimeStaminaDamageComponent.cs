// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Goobstation.Common.Stunnable;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause]
public sealed partial class OvertimeStaminaDamageComponent : Component
{
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate;

    /// <summary>
    ///     Total amount of stamina damage a person is about to get
    /// </summary>
    [DataField] public float Amount = 10f;

    [ViewVariables(VVAccess.ReadWrite)] public float Damage = 10f;

    /// <summary>
    ///     Divisor. How much damage should we add overtime.
    /// </summary>
    /// <remarks> For example, if the divisor is 5, out entity will get the entire overtime stam damage only after 5 seconds. </remarks>
    [DataField] public float Delta = 5f;
}
