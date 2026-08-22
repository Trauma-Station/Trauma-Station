// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Materials;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Syndicate.Components;

/// <summary>
/// This is used for a machine that converts normal items into their Syndicate variant(s).
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedSyndicateConverterSystem))]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class SyndicateConverterComponent : Component
{
    /// <summary>
    /// Whether or not conversion is occuring
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool Converting;

    /// <summary>
    /// The time at which conversion ends
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan ConversionEndTime;

    /// <summary>
    /// The current alertness towards the converter.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Alertness = 0;

    /// <summary>
    /// Amount of alertness needed for it to get detected. (Location announced)
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int DetectionAlertThreshold = 10;

    /// <summary>
    /// Amount of alertness needed for the station to go under emergency. (Station Alert Red, location re-announced)
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int EmergencyAlertThreshold = 25;

    /// <summary>
    /// Multiplier on conversion speed
    /// </summary>
    public float ConversionSpeedScale = 1;

    /// <summary>
    /// Multiplier on material costs
    /// </summary>
    public float MaterialCostScale = 1;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string SlotId = "item_slot";
}

[Serializable, NetSerializable]
public enum SyndicateConverterUIKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum SyndicateConverterVisuals : byte
{
    Packing
}

[Serializable, NetSerializable]
public sealed class SyndicateConverterStartPackBuiMessage : BoundUserInterfaceMessage
{

}
