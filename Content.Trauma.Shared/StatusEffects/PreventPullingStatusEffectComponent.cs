// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.StatusEffects;

/// <summary>
/// Status effect that prevents you from being pulled.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PreventPullingStatusEffectComponent : Component;
