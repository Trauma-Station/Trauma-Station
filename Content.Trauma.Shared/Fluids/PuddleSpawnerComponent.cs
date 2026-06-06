// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Components;

namespace Content.Trauma.Shared.Fluids;

/// <summary>
/// Creates or grows a puddle on this entity's tile then deletes itself.
/// </summary>
[EntityCategory("Spawner")]
[RegisterComponent, NetworkedComponent]
public sealed partial class PuddleSpawnerComponent : Component
{
    [DataField(required: true)]
    public Solution Solution = default!;
}
