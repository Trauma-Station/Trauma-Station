// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.AnimalAgeing;

/// <summary>
/// Animals with this component will age up a mob a "year" each ageing update
/// </summary>
[RegisterComponent, AutoGenerateComponentState, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class AnimalAgeingComponent : Component
{
    [DataField]
    public int AdultHoodYear = 10;

    [DataField]
    public int SeniorHoodYear = 30;

    [DataField]
    public int DeathYear = 35;

    [DataField, AutoNetworkedField]
    public int YearsOld;

    /// <summary>
    /// The time to age up
    /// </summary>
    [DataField]
    public TimeSpan AgeTime = TimeSpan.FromSeconds(20);

    [DataField]
    public int YearsPerUpdate = 1;

    [DataField, AutoNetworkedField]
    public AnimalAgeState CurrentAgeState = AnimalAgeState.Baby;

    [DataField, AutoPausedField]
    public TimeSpan NextAgeTime = TimeSpan.Zero;
}

[Serializable, NetSerializable]
public enum AnimalAgeState: byte
{
    Baby,
    Adult,
    Senior,
}
