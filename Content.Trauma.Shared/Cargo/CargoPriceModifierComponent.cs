// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Cargo;

/// <summary>
/// Station component that multiplies the price of cargo orders.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(CargoModifiersSystem))]
[AutoGenerateComponentState]
public sealed partial class CargoPriceModifierComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public float Modifier = 1f;
}
