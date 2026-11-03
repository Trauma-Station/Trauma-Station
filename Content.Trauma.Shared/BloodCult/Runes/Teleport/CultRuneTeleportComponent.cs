// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Shared.BloodCult.Runes.Teleport;

[RegisterComponent, NetworkedComponent]
public sealed partial class CultRuneTeleportComponent : Component
{
    [DataField]
    public float TeleportGatherRange = 0.65f;

    [DataField]
    public string Name = string.Empty;

    [DataField]
    public SoundPathSpecifier TeleportInSound = new("/Audio/_Trauma/BloodCult/veilin.ogg");

    [DataField]
    public SoundPathSpecifier TeleportOutSound = new("/Audio/_Trauma/BloodCult/veilout.ogg");
}
