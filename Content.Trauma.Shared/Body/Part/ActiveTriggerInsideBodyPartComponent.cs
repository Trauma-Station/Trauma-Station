// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Body.Part;

/// <summary>
/// Component added for <see cref="TriggerInsideBodyPartComponent"/> while inside a body part cavity.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(TriggerInsideBodyPartSystem))]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ActiveTriggerInsideBodyPartComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextTrigger;
}
