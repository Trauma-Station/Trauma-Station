// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.BloodCult;

namespace Content.Trauma.Server.BloodCult.Gamerule;

[RegisterComponent]
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
    [DataField]
    public bool TargetSacrificed;

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
