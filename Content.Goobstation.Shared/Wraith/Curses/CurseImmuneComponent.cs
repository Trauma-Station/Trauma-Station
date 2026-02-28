// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Wraith.Curses;

/// <summary>
/// Marks an entity as immune to getting curses
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CurseImmuneComponent : Component;
