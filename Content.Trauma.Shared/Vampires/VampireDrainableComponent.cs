// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Vampires;

/// <summary>
/// Marks an entity that can be drained by an entity with <see cref="VampireBloodsuckingComponent"/>.
///
/// If the bloodsucker sucks more than <see cref="MaxBlood"/> from this entity, then they cannot be drained anymore.
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
