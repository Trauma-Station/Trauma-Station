// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Goobstation.Shared.Fishing.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class FishingLureComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid FishingRod;

    [DataField, AutoNetworkedField]
    public EntityUid Fisher;

    [DataField, AutoNetworkedField]
    public EntityUid? AttachedEntity;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);
}
