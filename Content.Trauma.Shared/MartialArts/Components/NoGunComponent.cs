// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.MartialArts.Components;

/// <summary>
/// Used with martial arts, having this means that the user can't use a gun.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NoGunComponent : Component;
