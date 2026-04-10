// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Carrying;

[RegisterComponent, NetworkedComponent, Access(typeof(CarryingSystem))]
public sealed partial class CarriableComponent : Component
{
    /// <summary>
    /// Number of free hands required
    /// to carry the entity
    /// </summary>
    [DataField]
    public int FreeHandsRequired = 2;
}
