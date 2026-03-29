// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

<<<<<<<< HEAD:Content.Trauma.Common/Heretic/TargetInteractionRelayComponent.cs
namespace Content.Trauma.Common.Heretic;
========
namespace Content.Trauma.Common.Heretic;
>>>>>>>> upstream:Content.Trauma.Common/Heretic/TargetInteractionRelayComponent.cs

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
