// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Lavaland.Shared.Weapons.Block;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedBlockChargeSystem))]
[AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class BlockChargeComponent : Component
{
    /// <summary>
    /// Time between gaining block charges
    /// </summary>
    [DataField]
    public TimeSpan RechargeTime = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How much time is reduced from recharge when hitting a marked target
    /// </summary>
    [DataField]
    public TimeSpan MarkerReductionTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// When the next charge will be ready
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan NextCharge;

    /// <summary>
    /// Whether we currently have a charge ready
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HasCharge;
}
