// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class StarGazerComponent : Component
{
    [DataField]
    public ProtoId<FactionIconPrototype> MasterIcon = "GhoulHereticMaster";

    [DataField]
    public float MaxDistance = 20f;

    [ViewVariables, NonSerialized]
    public ICommonSession? ResettingMindSession;

    [DataField]
    public TimeSpan GhostRoleTime = TimeSpan.FromSeconds(20);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan GhostRoleTimer;

    [DataField]
    public TimeSpan ResetDistanceTime = TimeSpan.FromSeconds(5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan ResetDistanceTimer;

    [DataField]
    public EntProtoId TeleportEffect = "EffectCosmicCloud";

    [DataField]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/cosmic_energy.ogg");

    [DataField]
    public EntProtoId InactiveStatus = "StarGazerInactiveStatusEffect";
}
