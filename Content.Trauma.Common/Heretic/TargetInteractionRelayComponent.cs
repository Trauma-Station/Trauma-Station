// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Common.Heretic;

/// <summary>
/// Same as InteractionRelayComponent but relays target interactions, not user
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TargetInteractionRelayComponent : Component
{
    /// <summary>
    /// The entity the interactions are being relayed to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? RelayEntity;

    [DataField, AutoNetworkedField]
    public bool RelayMelee = true;

    [DataField, AutoNetworkedField]
    public bool RelayPulls = true;
}
