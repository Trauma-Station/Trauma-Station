// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Goobstation.Common.Weapons.DelayedKnockdown;

// TODO: make this a status effect entity
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class DelayedKnockdownComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan Started;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan NextKnockdown;

    [DataField]
    public TimeSpan Delay = TimeSpan.MaxValue;

    [DataField]
    public TimeSpan KnockdownTime;

    [DataField]
    public bool Refresh = true;
}
