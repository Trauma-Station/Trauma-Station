// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Shared.Blob.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class BlobFactoryComponent : Component
{
    [DataField]
    public float SpawnLimit = 3;

    [DataField]
    public EntProtoId<BlobMobComponent> Pod = "MobBlobPod";

    [DataField]
    public EntProtoId<BlobbernautComponent> BlobbernautId = "MobBlobBlobbernaut";

    [DataField]
    public EntityUid? Blobbernaut;

    /// <summary>
    /// Whether <see cref="Blobbernaut"/> exists serverside, client might not have it in PVS range.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HasBlobbernaut;

    [DataField, AutoNetworkedField]
    public List<EntityUid> BlobPods = new ();

    [DataField]
    public int Accumulator = 0;

    [DataField]
    public int AccumulateToSpawn = 3;
}
