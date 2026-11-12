// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Materials;
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
    [DataField]
    [AutoNetworkedField]
    public bool Converting;

    /// <summary>
    /// The time at which conversion ends
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan ConversionEndTime;

    /// <summary>
    /// The current alertness towards the converter.
    /// </summary>
    [DataField]
    public int Alertness = 0;

    /// <summary>
    /// Amount of alertness needed for it to get detected. (Location announced)
    /// </summary>
    [DataField]
    public int DetectionAlertThreshold = 10;

    public bool IsDetected = false;

    /// <summary>
    /// Amount of alertness needed for the station to go under emergency. (Station Alert Red, location re-announced)
    /// </summary>
    [DataField]
    public int EmergencyAlertThreshold = 25;

    public bool IsEmergency = false;

    /// <summary>
    /// Multiplier on conversion speed
    /// </summary>
    [DataField]
    public float ConversionTimeScale = 1;

    /// <summary>
    /// Multiplier on material costs
    /// </summary>
    [DataField]
    public float MaterialCostScale = 1;

    [DataField]
    public string SlotId = "item_slot";

    /// <summary>
    /// Power load when idle
    /// </summary>
    [DataField]
    public float BasePowerLoad = 1500;

    /// <summary>
    /// Power load when converting
    /// </summary>
    [DataField]
    public float ActivePowerLoad = 7500;

    /// <summary>
    /// Materials per Alert value based on category.
    /// </summary>
    public Dictionary<ProtoId<TagPrototype>, Dictionary<ProtoId<MaterialPrototype>, int>> ConversionCategoryList = new();
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
