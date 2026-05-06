// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Ash;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class CrackedLanternSummonComponent : Component
{
    [DataField]
    public EntityUid? Lantern;

    [DataField]
    public bool ShouldDespawn;

    [DataField]
    public EntityCoordinates? TargetCoords;

    [DataField]
    public EntityCoordinates? UserCoords;

    [DataField]
    public float DistThreshold;

    [DataField]
    public float SpeedMultiplier = 7f;

    [DataField]
    public float MaxSpeed = 20f;

    [DataField]
    public float MaxDistance = 10f;

    [DataField]
    public float MaxDisappearDistance = 16f;

    [DataField]
    public TimeSpan UpdateDelay = TimeSpan.FromMilliseconds(200);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate;

    [DataField]
    public SoundSpecifier TrailSound = new SoundPathSpecifier("/Audio/_Goobstation/Wizard/swap.ogg");

    [DataField]
    public EntProtoId TrailEffect = "SwapSpellEffect";

    [DataField]
    public SpriteSpecifier TrailSprite =
        new SpriteSpecifier.Rsi(new ResPath("/Textures/_Goobstation/Heretic/Mobs/hint.rsi"), "smol_progenitor");

    [DataField]
    public float Health = 35f;
}
