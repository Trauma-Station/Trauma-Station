// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.LinkedEntityContainer;

/// <summary>
/// Stores ent uid. Used with ToLinkedEntityTeleport and RandomTeleport EntityEffect.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LinkedEntityContainerComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Uid;
}
