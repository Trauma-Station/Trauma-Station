// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Interaction;

/// <summary>
/// Increases an entity's interaction range when using the entity-entity InRangeUnobstructed check.
/// Body compatible, each part adds its reach to the user.
/// </summary>
/// <remarks>
/// Won't work for anything that manually passes coords instead of using the entity-entity check.
/// </remarks>
[RegisterComponent, NetworkedComponent, Access(typeof(ExtraReachSystem))]
[AutoGenerateComponentState]
public sealed partial class ExtraReachComponent : Component
{
    /// <summary>
    /// Number added to reach.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Bonus;
}
