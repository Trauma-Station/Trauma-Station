// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Shared.BloodCult.Items.VeilShifter;

// TODO: reuse random teleport code
[RegisterComponent, NetworkedComponent]
public sealed partial class VeilShifterComponent : Component
{
    [DataField]
    public int TeleportDistanceMax = 10;

    [DataField]
    public int TeleportDistanceMin = 5;

    [DataField]
    public Vector2i Offset = Vector2i.One * 2;

    // How many times it will try to find safe location before aborting the operation?
    [DataField]
    public int Attempts = 10;

    [DataField]
    public SoundPathSpecifier? TeleportInSound = new("/Audio/_Trauma/BloodCult/veilin.ogg");

    [DataField]
    public SoundPathSpecifier? TeleportOutSound = new("/Audio/_Trauma/BloodCult/veilout.ogg");

    [DataField]
    public EntProtoId? TeleportInEffect = "CultTeleportInEffect";

    [DataField]
    public EntProtoId? TeleportOutEffect = "CultTeleportOutEffect";
}
