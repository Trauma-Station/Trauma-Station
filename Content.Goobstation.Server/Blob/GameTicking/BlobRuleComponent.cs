// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.GameTicking.Rules;
using Content.Shared.Mind;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes; // Trauma

namespace Content.Goobstation.Server.Blob.GameTicking;

[RegisterComponent, Access(typeof(BlobRuleSystem), typeof(BlobCoreSystem), typeof(BlobObserverSystem))]
public sealed partial class BlobRuleComponent : Component
{
    [DataField]
    public SoundSpecifier? DetectedAudio = new SoundPathSpecifier("/Audio/_Goobstation/Announcements/blob_detected.ogg");

    [DataField]
    public SoundSpecifier? CriticalAudio = new SoundPathSpecifier("/Audio/StationEvents/blobin_time.ogg");

    [ViewVariables]
    public List<(EntityUid mindId, MindComponent mind)> Blobs = new(); //BlobRoleComponent

    [ViewVariables]
    public BlobStage Stage = BlobStage.Default;

    [ViewVariables]
    public float Accumulator = 0f;

    /// <summary>
    /// Trauma - The shuttle event used for the blob CBurn autocall.
    /// </summary>
    [DataField]
    public EntProtoId BlobCBurnEvent = "SpawnCBURNNoAnnounce";

    /// <summary>
    /// Trauma - Whether or not a CBurn shuttle for blob has been sent.
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
