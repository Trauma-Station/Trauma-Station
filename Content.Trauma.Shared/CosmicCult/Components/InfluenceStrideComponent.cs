// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.CosmicCult.Components;

[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class InfluenceStrideComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan Expiry;
}
