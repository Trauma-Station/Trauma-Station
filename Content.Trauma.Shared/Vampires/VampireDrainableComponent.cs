// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Vampires;

/// <summary>
/// Marks an entity that can be drained by a vampire.
/// This means the entity with <see cref="VampireBloodsuckingComponent"/> can not drain more usable blood from them.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class VampireDrainableComponent : Component
{
    /// <summary>
    /// How much blood we have gathered from this entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int BloodGathered;

    /// <summary>
    /// The maximum amount of blood we are allowed to gather from this entity.
    /// </summary>
    [DataField]
    public int MaxBlood = 200;
};
