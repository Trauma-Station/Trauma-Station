// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class HereticCosmicRuneComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? LinkedRune;

    [DataField]
    public float Range = 0.75f;

    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/cosmic_energy.ogg");

    [DataField]
    public EntProtoId Effect = "HereticRuneCosmosLight";

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
    public TimeSpan NextUse = TimeSpan.Zero;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(2);
}
