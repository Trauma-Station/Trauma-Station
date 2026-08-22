// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Robust.Shared.Map;

namespace Content.Goobstation.Shared.Blob.Components;

[RegisterComponent]
public sealed partial class BlobObserverControllerComponent : Component
{
    [DataField]
    public EntityUid Blob;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(false)]
public sealed partial class BlobObserverComponent : Component
{
    public override bool SendOnlyToOwner => true;

    [ViewVariables]
    public bool IsProcessingMoveEvent;

    [DataField, AutoNetworkedField]
    public EntityUid? Core;

    [DataField, AutoNetworkedField]
    public ProtoId<BlobChemPrototype> SelectedChemId = "ReactiveSpines";

    [DataField, AutoNetworkedField]
    public EntityUid VirtualItem;
}

[Serializable, NetSerializable]
public sealed class BlobSetChemMessage(ProtoId<BlobChemPrototype> chem) : BoundUserInterfaceMessage
{
    public readonly ProtoId<BlobChemPrototype> Chem = chem;
}

[Serializable, NetSerializable]
public enum BlobChemSwapUiKey : byte
{
    Key
}

public sealed partial class BlobTransformTileActionEvent : WorldTargetActionEvent
{
    /// <summary>
    /// Type of tile that can be transformed.
    /// Will be ignored if null.
    /// </summary>
    [DataField]
    public ProtoId<BlobTilePrototype>? TransformFrom = "Normal";

    /// <summary>
    /// Type of the resulting tile.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<BlobTilePrototype> TileType;

    /// <summary>
    /// Does this tile requires node nearby.
    /// </summary>
    [DataField]
    public bool RequireNode = true;
}

public sealed partial class BlobCreateBlobbernautActionEvent : WorldTargetActionEvent;

public sealed partial class BlobSplitCoreActionEvent : WorldTargetActionEvent;

public sealed partial class BlobSwapCoreActionEvent : WorldTargetActionEvent;

public sealed partial class BlobToCoreActionEvent : InstantActionEvent;

public sealed partial class BlobSwapChemActionEvent : InstantActionEvent;
