// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.StatusIcon;

namespace Content.Trauma.Shared.Xenomorphs.Infection;

[RegisterComponent, NetworkedComponent]
public sealed partial class XenomorphInfectionComponent : Component
{
    /// <summary>
    /// A set of prototype IDs for status icons representing different growth stages of the infection.
    /// </summary>
    [DataField]
    public Dictionary<int, ProtoId<StatusIconPrototype>> InfectedIcons = new();

    /// <summary>
    /// Current stage of infection development.
    /// </summary>
    [DataField]
    public int GrowthStage;

    [DataField]
    public int MaxGrowthStage = 1;

    [DataField]
    public EntProtoId LarvaPrototype = "MobXenomorphLarva";

    /// <summary>
    /// The probability of infection growth per GrowTime.
    /// </summary>
    [DataField]
    public float GrowProb = 1f;

    /// <summary>
    /// The time required for infection to grow.
    /// </summary>
    [DataField]
    public TimeSpan GrowTime = TimeSpan.FromSeconds(25);

    [DataField]
    public Dictionary<int, EntityEffect[]> Effects = new ();

    [DataField]
    public EntityUid? SourceMindId;

    [ViewVariables]
    public TimeSpan NextPointsAt;

    [ViewVariables]
    public EntityUid? Infected;
}
