// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.EmptyScroll;

/// <summary>
/// Paper that acts like funger empty scroll when you write on it with a pen.
/// If you write a nonexisting prayer misspell anything etc, you get nothing.
/// Any prayer from a <see cref="ScrollPlayerPrototype"/> can be used.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EmptyScrollComponent : Component;
