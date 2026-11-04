// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.BloodCult.BloodRites;

[RegisterComponent, NetworkedComponent, Access(typeof(BloodRitesSystem))]
[AutoGenerateComponentState]
public sealed partial class BloodRitesAuraComponent : Component
{
    /// <summary>
    /// Total blood stored in the Aura.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 StoredBlood;

    /// <summary>
    /// True while extracting blood from a mob.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Extracting;

    /// <summary>
    /// Ratio which is applied to calculate the <see cref="StoredBlood"/> amount to regenerate blood in someone.
    /// </summary>
    [DataField]
    public float BloodRegenerationRatio = 0.5f;

    /// <summary>
    /// How units of blood do we need to heal 1 point of damage
    /// </summary>
    [DataField]
    public float HealRatio = 2f;

    /// <summary>
    /// Ratio which is applied to calculate the <see cref="StoredBlood"/> amount to heal yourself.
    /// </summary>
    [DataField]
    public float SelfHealRatio = 2f;

    /// <summary>
    /// The amount of blood that is extracted from a person on using it on them.
    /// </summary>
    [DataField]
    public FixedPoint2 BloodExtractionAmount = 50f;

    /// <summary>
    /// Time required to extract blood of something with bloodstream.
    /// </summary>
    [DataField]
    public TimeSpan BloodExtractionTime = TimeSpan.FromSeconds(1);

    [DataField]
    public SoundSpecifier BloodExtractSound = new SoundPathSpecifier("/Audio/_Trauma/BloodCult/enter_blood.ogg");

    /// <summary>
    /// Items that can be crafted using stored blood, and how much they cost.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<EntProtoId, FixedPoint2> Crafts = new();

    /// <summary>
    /// Visual beam effect sprite for draining blood or healing teammates
    /// </summary>
    [DataField]
    public SpriteSpecifier JointSprite = new SpriteSpecifier.Rsi(new ResPath("/Textures/_Trauma/BloodCult/Effects/beam.rsi"), "sendbeam");

    [DataField]
    public TimeSpan JointDuration = TimeSpan.FromSeconds(1);

    [DataField]
    public float JointRange = 5f;

    /// <summary>
    /// Visual effect that appears on every puddle that we drain blood from
    /// </summary>
    [DataField]
    public EntProtoId PuddleDrainEffect = "CultTileSpawnEffect";

    /// <summary>
    /// Invisible entity with 1s despawn that we hook up our beam to when drawing blood from puddles
    /// Needed because puddle may be deleted after we drain blood from it
    /// </summary>
    [DataField]
    public EntProtoId PuddleDrainBeamEntity = "PuddleDrainBeamEntity";
}
