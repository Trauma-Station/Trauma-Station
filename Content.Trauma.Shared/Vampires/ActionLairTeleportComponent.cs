// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;

namespace Content.Trauma.Shared.Vampires;

/// <summary>
/// Action that can only be used in crit/death to teleport to your lair.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ActionLairTeleportComponent : Component
{
    /// <summary>
    /// The lair to teleport to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Lair;

    [DataField]
    public string StorageId = "entity_storage";
}

public sealed partial class ActionLairTeleportEvent : InstantActionEvent;
