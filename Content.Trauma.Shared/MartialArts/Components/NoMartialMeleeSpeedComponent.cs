// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.MartialArts.Components;

/// <summary>
/// Put on a weapon to stop martial arts speeding up attacks made with it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NoMartialMeleeSpeedComponent : Component;
