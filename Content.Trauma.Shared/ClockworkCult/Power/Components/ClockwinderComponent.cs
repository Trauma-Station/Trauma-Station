// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.ClockworkCult.Power.Components;

/// <summary>
/// Connects entities with <see cref="ClockworkTransferrerComponent"/> to entities with <see cref="ClockworkStructureComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ClockwinderComponent : Component
{
    /// <summary>
    /// The current selected transferrer
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Transferrer;
}
