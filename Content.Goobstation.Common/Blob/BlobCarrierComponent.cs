// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Goobstation.Common.Blob;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause]
public sealed partial class BlobCarrierComponent : Component
{
    public override bool SendOnlyToOwner => true;

    [DataField]
    public float TransformationDelay = 600;

    [DataField]
    public float AlertInterval = 30f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextAlert;

    [DataField]
    public bool HasMind;

    [ViewVariables(VVAccess.ReadWrite)]
    public float TransformationTimer = 0;

    [DataField]
    public EntProtoId CoreBlobPrototype = "CoreBlobTile";

    [DataField]
    public EntityUid? TransformToBlob = null;
}
