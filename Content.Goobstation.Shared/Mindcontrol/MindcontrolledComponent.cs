// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusIcon;
using Robust.Shared.Audio;

namespace Content.Goobstation.Shared.Mindcontrol;

[RegisterComponent, NetworkedComponent]
public sealed partial class MindcontrolledComponent : Component
{
    [DataField]
    public EntityUid? Master;
    [DataField]
    public SoundSpecifier MindcontrolStartSound = new SoundPathSpecifier("/Audio/_Goobstation/Ambience/Antag/mindcontrol_start.ogg");
    [DataField]
    public bool BriefingSent;

    [DataField]
    public ProtoId<StatusIconPrototype> MindcontrolIcon = "MindcontrolledFaction";
}
