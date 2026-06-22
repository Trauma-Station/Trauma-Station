// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Cargo;

/// <summary>
/// Station component that multiplies the reward for its cargo bounties.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(CargoModifiersSystem))]
[AutoGenerateComponentState]
public sealed partial class BountyModifierComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public float Modifier = 1f;
}
