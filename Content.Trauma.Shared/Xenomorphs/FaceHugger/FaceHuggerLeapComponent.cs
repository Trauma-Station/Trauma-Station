// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Xenomorphs.FaceHugger;

/// <summary>
/// Handles the leap action for sentient facehuggers
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FaceHuggerLeapComponent : Component
{
    [DataField]
    public EntityUid? LeapActionEntity;

    [DataField]
    public EntProtoId LeapAction = "ActionFaceHuggerLeap";

    [DataField]
    public float LeapSpeed = 6f;

    [DataField]
    public SoundSpecifier? LeapSound = new SoundPathSpecifier("/Audio/Animals/Blob/blobattack.ogg");

    [DataField, AutoNetworkedField]
    public bool IsLeaping;
}
