// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Vampires.Gargantua;

/// <summary>
/// Component applied to a Gargantua Vampire during a charge action.
/// Applies entity effects on collision with other entities, and gets removed during a landing event.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class GargantuaChargingComponent : Component
{
    /// <summary>
    /// The effect to run on targets we collided with
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<EntityEffectPrototype> Effect = default!;

    /// <summary>
    /// Delete after few seconds, if component hasn't been deleted.
    /// </summary>
    [DataField]
    public TimeSpan Delete = TimeSpan.FromSeconds(0.8f);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    [AutoNetworkedField]
    public TimeSpan NextDelete;
}
