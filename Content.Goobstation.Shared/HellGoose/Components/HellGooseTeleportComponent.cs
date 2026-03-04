// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.HellGoose.Components;

/// <summary>
/// Component for the hell goose teleporting back to the station
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HellGooseTeleportComponent : Component;
