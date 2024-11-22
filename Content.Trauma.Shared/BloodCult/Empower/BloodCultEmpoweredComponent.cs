// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Alert;
using Content.Shared.Maps;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.BloodCult.Empower;

// TODO: change this to be a status effect?
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause]
public sealed partial class BloodCultEmpoweredComponent : Component
{
    /// <summary>
    ///     Changes the damage from drawing/using runes.
    /// </summary>
    [DataField]
    public float RuneDamageMultiplier = 0.5f;

    /// <summary>
    ///     Changes the drawing time of runes.
    /// </summary>
    [DataField]
    public float RuneTimeMultiplier = 0.5f;

    /// <summary>
    ///     Increases the amount of spells cultists can create at once.
    /// </summary>
    [DataField]
    public int ExtraSpells = 3;

    /// <summary>
    /// How long empowering lasts.
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(20);

    /// <summary>
    /// When empowering will end.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan EmpowerEnd;

    [DataField]
    public float NearbyCultTileRadius = 1f;

    [DataField]
    public ProtoId<ContentTileDefinition> CultTile = "CultFloor";

    [DataField]
    public ProtoId<AlertPrototype> EmpoweredAlert = "CultEmpowered";
}
