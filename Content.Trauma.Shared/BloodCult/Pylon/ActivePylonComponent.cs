// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.BloodCult.Pylon;

/// <summary>
/// Component added to active pylons.
/// Can be interacted with by cultists to toggle it.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(PylonSystem))]
[AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class ActivePylonComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan NextCorrupt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan NextHeal;
}
