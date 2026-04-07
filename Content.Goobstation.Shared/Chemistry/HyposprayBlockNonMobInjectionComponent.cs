// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Shared.Chemistry;

/// <summary>
/// Prevents this injector from injecting into non mobs.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HyposprayBlockNonMobInjectionComponent : Component;
