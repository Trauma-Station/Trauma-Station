// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Changeling.Components;

/// <summary>
/// Component added to bodies that a changeling absorbed.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AbsorbedComponent : Component;
