// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.EmptyScroll;

/// <summary>
/// Sets this entity's paper text to a random <see cref="ScrollPrayerPrototype"/>'s text on mapinit.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RandomScrollComponent : Component;
