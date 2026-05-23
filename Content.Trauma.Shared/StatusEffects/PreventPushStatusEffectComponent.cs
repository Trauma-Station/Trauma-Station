// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.StatusEffects;

/// <summary>
/// Status effect that prevents you from being pushed/thrown.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PreventPushStatusEffectComponent : Component;
