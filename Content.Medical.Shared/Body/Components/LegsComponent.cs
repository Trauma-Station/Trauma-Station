// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Medical.Shared.Body;

/// <summary>
/// Body component that tracks the mob's legs.
/// Prevents things like standing up without any legs.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(LegsSystem))]
[AutoGenerateComponentState]
public sealed partial class LegsComponent : Component
{
    /// <summary>
    /// All leg organs on the body.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Legs = new(2);

    /// <summary>
    /// How many legs it takes to walk.
    /// </summary>
    [DataField]
    public int Required = 2;
}
