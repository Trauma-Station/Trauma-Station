// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Megafauna;

/// <summary>
/// Megafauna component that runs effects on the entity when a phase changes
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MegafaunaPhasesEffectsComponent : Component
{
    [DataField(required: true)]
    public Dictionary<int, EntityEffect[]> PhaseEffects = new();
}
