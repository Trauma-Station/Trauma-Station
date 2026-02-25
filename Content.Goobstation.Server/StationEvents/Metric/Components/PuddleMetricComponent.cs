// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.StationEvents.Metric.Components;

[RegisterComponent, Access(typeof(PuddleMetricSystem))]
public sealed partial class PuddleMetricComponent : Component
{
    // Impact Constants
    private const float MinimalImpact = 0.02f;
    private const float MinorImpact = 0.1f;
    private const float ModerateImpact = 0.2f;
    private const float MajorImpact = 0.3f;

    /// <summary>
    ///   The cost of each puddle, per mL. Note about 200 mL is one puddle.
    ///   Example: A water puddle of 200mL would contribute (200 * 0.02) = 4 chaos points.
    /// </summary>
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> Puddles = new()
    {
        { "Water", MinimalImpact },
        { "SpaceCleaner", MinimalImpact },

        { "Nutriment", MinorImpact },
        { "Sugar", MinorImpact },
        { "Ephedrine", MinorImpact },
        { "Ale", MinorImpact },
        { "Beer", ModerateImpact },

        { "Slime", ModerateImpact },
        { "Blood", ModerateImpact },
        { "CopperBlood", ModerateImpact },
        { "BlackBlood", ModerateImpact },
        { "ZombieBlood", ModerateImpact },
        { "AmmoniaBlood", ModerateImpact },
        { "ChangelingBlood", ModerateImpact },
        { "SpaceDrugs", MajorImpact },
        { "SpaceLube", MajorImpact },
        { "SpaceGlue", MajorImpact },
    };

    [DataField]
    public FixedPoint2 PuddleDefault = 0.1f;

}
