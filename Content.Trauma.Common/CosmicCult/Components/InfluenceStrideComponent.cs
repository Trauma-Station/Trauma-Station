// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.CosmicCult.Components;

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class InfluenceStrideComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan Expiry;
}
