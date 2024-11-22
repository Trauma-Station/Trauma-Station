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
    public float BloodRegenerationRatio = 0.1f;

    /// <summary>
    /// Ratio which is applied to calculate the <see cref="StoredBlood"/> amount to heal yourself.
    /// </summary>
    [DataField]
    public float SelfHealRatio = 2f;

    /// <summary>
    /// The amount of blood that is extracted from a person on using it on them.
    /// </summary>
    [DataField]
    public FixedPoint2 BloodExtractionAmount = 30f;

    /// <summary>
    /// Time required to extract blood of something with bloodstream.
    /// </summary>
    [DataField]
    public TimeSpan BloodExtractionTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How much <see cref="StoredBlood"/> is consumed on healing a cultist.
    /// </summary>
    [DataField]
    public FixedPoint2 HealingCost = 40;

    /// <summary>
    /// How much damage each use of the hand will heal. Will heal literally anything. Nar'sien magic, you know.
    /// </summary>
    [DataField]
    public FixedPoint2 TotalHealing = 20;

    [DataField]
    public SoundSpecifier BloodRitesAudio = new SoundPathSpecifier(
        new ResPath("/Audio/_Trauma/BloodCult/rites.ogg"),
        AudioParams.Default.WithVolume(-3));

    /// <summary>
    /// Items that can be crafted using stored blood, and how much they cost.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<EntProtoId, FixedPoint2> Crafts = new();
}
