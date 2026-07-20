// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Blob.Components;

namespace Content.Goobstation.Server.Blob.Objectives;

[RegisterComponent]
public sealed partial class BlobCaptureConditionComponent : Component
{
    [DataField]
    public int Target = StationBlobConfigComponent.DefaultStageEnd;
}
