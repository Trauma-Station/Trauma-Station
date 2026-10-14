// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Shared.BloodCult.Items.BloodSpear;

[RegisterComponent, NetworkedComponent, Access(typeof(BloodSpearSystem))]
[AutoGenerateComponentState]
public sealed partial class BloodSpearComponent : Component
{
    /// <summary>
    /// The cultist this spear is bound to, can be recalled by them.
    /// Only exists on server and for the client that bound it.
    /// </summary>
    [DataField]
    public EntityUid? Master;

    /// <summary>
    /// Networked for everyone to know if a spear is bound or not for prediction.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HasMaster;

    [DataField]
    public TimeSpan ParalyzeTime = TimeSpan.FromSeconds(4);

    [DataField]
    public EntProtoId RecallActionId = "ActionBloodSpearRecall";

    [DataField, AutoNetworkedField]
    public EntityUid? RecallAction;

    [DataField]
    public SoundSpecifier RecallAudio = new SoundPathSpecifier(
        new ResPath("/Audio/_Trauma/BloodCult/rites.ogg"),
        AudioParams.Default.WithVolume(-3));
}
