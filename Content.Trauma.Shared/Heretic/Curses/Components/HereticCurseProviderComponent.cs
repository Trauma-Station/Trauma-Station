// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Heretic.Curses.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class HereticCurseProviderComponent : Component
{
    [DataField]
    public float MaxBloodMultiplier = 4f;

    [DataField]
    public float MaxBloodAmount = 10f;

    [DataField(required: true)]
    public Dictionary<EntProtoId, CurseProviderData> CursePrototypes;

    [DataField]
    public EntProtoId CursedStatusEffect = "StatusEffectCursed";

    [DataField]
    public TimeSpan CurseDelay = TimeSpan.FromMinutes(5);

    [DataField]
    public SoundSpecifier? CurseSound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/curse.ogg");
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class CurseProviderData
{
    [DataField(required: true)]
    public TimeSpan Time = TimeSpan.Zero;

    [DataField]
    public bool Silent;
}
