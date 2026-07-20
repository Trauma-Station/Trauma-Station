// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.ListEntitySelector;

[Serializable, NetSerializable]
public sealed class ListEntitySelectorMessage(NetEntity selectedEntity) : BoundUserInterfaceMessage
{
    /// <summary>
    /// The entity that was clicked by the user.
    /// </summary>
    public NetEntity SelectedEntity = selectedEntity;
}

[Serializable, NetSerializable]
public sealed class ListEntitySelectorState(HashSet<NetEntity> entities, string title) : BoundUserInterfaceState
{
    /// <summary>
    /// The entities to pass to the BUI.
    /// </summary>
    public HashSet<NetEntity> Entities = entities;

    /// <summary>
    /// The title of the window. Overrides the default one.
    /// </summary>
    public string Title = title;
}

[Serializable, NetSerializable]
public enum ListEntitySelectorUiKey : byte
{
    Key
}
