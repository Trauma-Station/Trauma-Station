// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.CosmicCult.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedMonumentSystem))]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class MonumentTransformingComponent : Component
{
    /// <summary>
    /// The time when insertion ends.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan EndTime;
}
