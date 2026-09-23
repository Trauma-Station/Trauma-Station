// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.BloodCult.Gamerule;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class BloodCultRuleComponent : Component
{
    [DataField]
    public EntProtoId HarvesterPrototype = "ConstructHarvester";

    [DataField]
    public Color EyeColor = Color.FromHex("#f80000");

    [DataField]
    public int ReadEyeThreshold = 5;

    [DataField]
    public int PentagramThreshold = 8;

    [DataField]
    public bool LeaderSelected;

    /// <summary>
    /// The current player that Nar'Sie wants sacraficed.
    /// </summary>
    [DataField]
    public EntityUid? OfferingTarget;

    /// <summary>
    /// Set to true when the target is sacrificed, allowing the Nar'Sie summoning ritual.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool TargetSacrificed;

    /// <summary>
    /// Set to true after summoning Nar'Sie, used for the objective.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool NarSieSummoned;

    /// <summary>
    /// Allowed areas for area-specific runes.
    /// Consumed when they are placed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntProtoId> RitualAreas = new();

    /// <summary>
    /// Possible areas to pick for <see cref="Areas"/>.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> AreaPool = new();

    /// <summary>
    /// Number of aras to pick from <see cref="AreaPool"/>.
    /// </summary>
    [DataField]
    public int AreaCount = 3;

    [DataField]
    public EntityUid? CultLeader;

    [DataField]
    public CultStage Stage = CultStage.Start;

    [DataField]
    public CultWinCondition WinCondition = CultWinCondition.Draw;

    [DataField]
    public List<EntityUid> Cultists = new();

    [DataField]
    public List<EntityUid> Constructs = new();
}
