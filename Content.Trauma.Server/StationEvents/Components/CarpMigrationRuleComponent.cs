// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.StationEvents.Components;

/// <summary>
/// Handles spawning logic for the carp migration event.
/// Requires <c>SpaceSpawnRuleComponent</c> for picking the carp spawn location.
/// </summary>
[RegisterComponent]
public sealed partial class CarpMigrationRuleComponent : Component
{
    [DataField]
    public EntProtoId Proto = "MobCarpMigrating";

    [DataField]
    public int Min = 2;

    [DataField]
    public int Max = 4;
}
