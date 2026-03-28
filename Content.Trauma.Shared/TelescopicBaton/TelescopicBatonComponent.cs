// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.TelescopicBaton;

[RegisterComponent, NetworkedComponent, Access(typeof(TelescopicBatonSystem))]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class TelescopicBatonComponent : Component
{
    [DataField]
    public bool AlwaysDropItems;

    /// <summary>
    ///     The amount of time during which the baton will be able to knockdown someone after activating it.
    /// </summary>
    [DataField]
    public TimeSpan AttackTimeframe = TimeSpan.FromSeconds(1.8f);

    /// <summary>
    /// If this is in the future, the next attack will drop the target's items.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan NextAttack = TimeSpan.Zero;
}
