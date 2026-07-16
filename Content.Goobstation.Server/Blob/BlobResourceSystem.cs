// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Blob;
using Content.Goobstation.Shared.Blob.Components;
using Content.Server.GameTicking;

namespace Content.Goobstation.Server.Blob;

public sealed partial class BlobResourceSystem : SharedBlobResourceSystem
{
    /// <summary>
    /// On round end makes all the blobs resource nodes generate 100 points each pulse.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnRoundEnd(RoundEndTextAppendEvent args)
    {
        var query = EntityQueryEnumerator<BlobResourceComponent>();
        foreach (var ent in query)
        {
            ent.Comp.PointsPerPulsed = 100;
            Dirty(ent);
        }
    }
}
