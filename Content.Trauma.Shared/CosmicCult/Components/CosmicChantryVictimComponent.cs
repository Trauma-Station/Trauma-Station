// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.CosmicCult.Components;

/// <summary>
/// Component for targets trapped in a vacuous chantry
/// </summary>
[RegisterComponent]
public sealed partial class CosmicChantryVictimComponent : Component
{
    [ViewVariables] public Entity<CosmicChantryComponent> Chantry = default!;

    [ViewVariables] public bool WasGhostRole;

    [ViewVariables] public bool WasGhostTakeoverAvailable;
}
