// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.StatusEffects;

/// <summary>
/// Status effect that makes you unable to shoot guns.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class UnableToShootStatusEffectComponent : Component;
