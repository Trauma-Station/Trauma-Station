// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Damage;

/// <summary>
/// Component for a mob or its worn gloves to be immune to hot potato, etc's DamageOnHolding.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DamageOnHoldingImmuneComponent : Component;
