// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.ChangeOnCool;

[RegisterComponent]
public sealed partial class ChangeOnCoolComponent : Component
{
    /// <summary>
    /// The temperature at which the entity is replaced the cooled version.
    /// </summary>
    [DataField]
    public float CoolTemp = 100f;

    /// <summary>
    /// The new entity that replaces the cooled entity.
    /// </summary>
    [DataField]
    public EntProtoId CooledPrototype = "BrassBloomCold";

    /// <summary>
    /// The popup when the entity reaches the cooled temperature.
    /// </summary>
    [DataField]
    public LocId CooledPopup = "cooled-popup-text";
}
