// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Genetics.Abilities;

/// <summary>
/// Component for a projectile that adds the chemspike transfer action while embedded in a mob.
/// Needs <c>ActionProjectileComponent</c> to be set, and the action's container to be a mutation.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChemTransferProjectileComponent : Component;
