// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.BloodCult.Runes.Apocalypse;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class CultRuneApocalypseComponent : Component
{
    [DataField]
    public TimeSpan InvokeTime = TimeSpan.FromSeconds(20);

    /// <summary>
    ///     If cult has less than this percent of current server population,
    ///     one of the possible events will be triggered.
    /// </summary>
    [DataField]
    public float CultistsThreshold = 0.15f;

    [DataField]
    public float EmpRange = 30f;

    [DataField]
    public float EmpEnergyConsumption = 10000;

    [DataField]
    public TimeSpan EmpDuration = TimeSpan.FromMinutes(3);

    /// <summary>
    /// Was the rune already used or not.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Used;

    [DataField]
    public Color UsedColor = Color.DimGray;

    /// <summary>
    ///     These events will be triggered on each rune activation.
    /// </summary>
    [DataField]
    public List<EntProtoId> GuaranteedEvents = new()
    {
        "PowerGridCheck",
        "SolarFlare"
    };

    /// <summary>
    ///     One of these events will be selected on each rune activation.
    ///     Stores the event and how many times it should be repeated.
    /// </summary>
    [DataField]
    public Dictionary<EntProtoId, int> PossibleEvents = new()
    {
        ["ImmovableRodSpawn"] = 3,
        ["MimicVendorRule"] = 2,
        ["KingRatMigration"] = 2,
        ["MeteorSwarm"] = 2,
        ["SpiderSpawnHorde"] = 3, // more spiders
        ["AnomalySpawn"] = 4,
        ["KudzuGrowth"] = 2,
    };
}

[Serializable, NetSerializable]
public enum ApocalypseRuneVisuals : byte
{
    Used,
    Layer
}
