// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Blob;
using Content.Shared.Mind;
using Robust.Shared.Audio;

namespace Content.Goobstation.Shared.GameTicking.Rules;

[RegisterComponent]
public sealed partial class BlobRuleComponent : Component
{
    [DataField]
    public SoundSpecifier? DetectedAudio = new SoundPathSpecifier("/Audio/_Goobstation/Announcements/blob_detected.ogg");

    [DataField]
    public SoundSpecifier? CriticalAudio = new SoundPathSpecifier("/Audio/_Trauma/StationEvents/assimilation.ogg");

    [DataField]
    public BlobStage Stage = BlobStage.Default;

    // TODO: kill
    [ViewVariables]
    public float Accumulator = 0f;

    /// <summary>
    /// The shuttle event used for the blob CBurn autocall.
    /// </summary>
    [DataField]
    public EntProtoId BlobCBurnEvent = "SpawnCBURNNoAnnounce";

    /// <summary>
    /// Whether or not a CBurn shuttle for blob has been sent.
    /// </summary>
    [DataField]
    public bool BlobCBurnCalled;
}

public enum BlobStage : byte
{
    Default,
    Begin,
    Critical,
    TheEnd,
}
