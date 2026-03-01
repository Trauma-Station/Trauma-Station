// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Chemistry;

/// <summary>
/// Makes a injector use <see cref="SolutionCartridgeComponent"/> items for its solution.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CartridgeInjectorComponent : Component;
