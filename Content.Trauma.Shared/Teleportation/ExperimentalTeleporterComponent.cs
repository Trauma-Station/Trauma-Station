// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Teleportation;

[RegisterComponent, NetworkedComponent, Access(typeof(ExperimentalTeleporterSystem))]
public sealed partial class ExperimentalTeleporterComponent : Component
{
    [DataField]
    public int MinTeleportRange = 3;

    [DataField]
    public int MaxTeleportRange = 8;

    [DataField]
    public int EmergencyLength = 4;

    [DataField]
    public List<Angle> RandomRotations = new() { Angle.FromDegrees(90), Angle.FromDegrees(-90) };

    [DataField]
    public EntProtoId TeleportInEffect = "ExperimentalTeleporterInEffect";

    [DataField]
    public EntProtoId TeleportOutEffect = "ExperimentalTeleporterOutEffect";

    [DataField]
    public SoundSpecifier? TeleportSound = new SoundPathSpecifier("/Audio/_White/Object/Devices/experimentalsyndicateteleport.ogg");
}
