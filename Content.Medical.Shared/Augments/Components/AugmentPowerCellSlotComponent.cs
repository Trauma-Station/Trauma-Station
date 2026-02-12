using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.Augments;

/// <summary>
/// Component for entities that serve as AugmentPowerCellSlot organs
/// <summary>
[RegisterComponent, NetworkedComponent, Access(typeof(AugmentPowerCellSystem))]
public sealed partial class AugmentPowerCellSlotComponent : Component
{
    /// <summary>
    /// The alert shown with the power level when a battery is installed.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> BatteryAlert = "AugmentBattery";

    /// <summary>
    /// The alert shown when no battery is installed.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> NoBatteryAlert = "BorgBatteryNone";
}

/// <summary>
/// Marker component to indicate that an entity currently has an AugmentPowerCellSlot organ
/// <summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HasAugmentPowerCellSlotComponent : Component;

/// <summary>
/// Alert event that tells the player their battery charge + power usage when clicked.
/// </summary>
public sealed partial class AugmentBatteryAlertEvent : BaseAlertEvent;
