// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.LinkedEntityContainer;

/// <summary>
/// Spawns an ent on startup and stores its uid. Used with ToLinkedEntityTeleport EntityEffect.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LinkedEntityContainerComponent : Component
{
    [DataField, AutoNetworkedField]
    public NetEntity NetEnt;

    /// <summary>
    /// This flag ensures that only one instance of LinkedEntity will be spawned.
    /// </summary>
    public bool Spawned = false;
}
