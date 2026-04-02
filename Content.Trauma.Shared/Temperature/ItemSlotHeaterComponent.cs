// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Temperature;

/// <summary>
/// Heats entities inside an item slot
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ItemSlotHeaterComponent : Component
{
    /// <summary>
    /// The slot to heat
    /// </summary>
    [DataField(required: true)]
    public string Slot;

    /// <summary>
    /// The heat to apply to the entity in Kelvin per <see cref="Update"/> period.
    /// </summary>
    [DataField]
    public float Temp = 100f;

    /// <summary>
    /// The max temperature the item can have.
    /// This changes to minimum if <see cref="Temp"/> is negative.
    /// </summary>
    [DataField]
    public float MaxTemp = 300f;

    /// <summary>
    /// How often to update the heating
    /// </summary>
    [DataField]
    public TimeSpan Update = TimeSpan.FromSeconds(1);
}
