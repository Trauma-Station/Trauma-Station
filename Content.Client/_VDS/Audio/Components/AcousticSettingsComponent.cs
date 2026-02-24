using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Client._VDS.Audio.Components;

/// <summary>
/// Holds client-side settings for <see cref="AcousticDataSystem"/> that the player
/// should not be able to normally adjust.
/// </summary>
[RegisterComponent]
[Access(typeof(AcousticDataSystem))]
public sealed partial class AcousticSettingsComponent : Component
{
    /// <summary>
    /// A list of distances and what <see cref="AudioPresetPrototype"/> to use alongside it.
    /// </summary>
    [DataField, ViewVariables]
    public SortedList<float, ProtoId<AudioPresetPrototype>> ReverbPresets = new()
    {
        { 10f, "SpaceStationCupboard" },
        { 13f, "DustyRoom" },
        { 15f, "SpaceStationSmallRoom" },
        { 18f, "SpaceStationShortPassage" },
        { 23f, "SpaceStationMediumRoom" },
        { 28f, "SpaceStationHall" },
        { 35f, "SpaceStationLargeRoom" },
        { 40f, "Auditorium" },
        { 45f, "ConcertHall" },
        { 70f, "Hangar" },
    };

    /// <summary>
    /// Based on the maximum posssible distance an acoustic raycast can travel,
    /// what percentage a single segment of it can it travel before it is considered 'escaped' and terminated early?
    /// </summary>
    [DataField, ViewVariables]
    public float EscapeDistancePercentage = 0.3f;

    /// <summary>
    /// We will never penalize our acoustic data less than this percentage.
    /// </summary>
    [DataField, ViewVariables]
    public float MaxmimumEscapePenalty = 0.10f;

    /// <summary>
    /// Penalize the all of the acoustic data by this percentage if the client is standing in
    /// an unrooved area.
    /// </summary>
    [DataField, ViewVariables]
    public float NoRoofPenalty = 0.10f;

    /// <summary>
    /// Maximum random degree offset an acoustic ray may take each bounce.
    /// Note that this is applied both clock-wise and counter-clockwise.
    /// </summary>
    [DataField, ViewVariables]
    public float DirectionRandomOffset = 0.3f;

    /// <summary>
    /// How large our absorption modifier is allowed to get.
    /// Values above 1.0f allow negative <see cref="Content.Shared._VDS.Audio.Components.AcousticDataComponent.Absorption"/> values
    /// to amplify the acoustic magnitude.
    /// </summary>
    [DataField, ViewVariables]
    public float MaxAbsorptionClamp = 1.3f;

    /// <summary>
    /// How much blending we do via lerp for our previous and current average magnitude values.
    /// </summary>
    [DataField, ViewVariables]
    public float AvgMagnitudeBlend = 0.25f;
}
