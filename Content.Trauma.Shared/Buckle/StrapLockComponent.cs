// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Trauma.Shared.EntityEffects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Buckle;

/// <summary>
/// Prevents unbuckling the buckled mob when enabled by other code or entity effects.
/// Also makes it so buckling someone while unlocked occupies both of your hands,
/// until either the strap gets locked or you stop holding the person up.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(StrapLockSystem))]
[AutoGenerateComponentState]
public sealed partial class StrapLockComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Locked;

    /// <summary>
    /// How many free hands are needed to strap someone.
    /// </summary>
    [DataField]
    public int RequiredHands = 2;

    /// <summary>
    /// The entity effect prototype to run on the buckled person if dropped while unlocked.
    /// </summary>
    [DataField]
    public ProtoId<EntityEffectPrototype> DropEffect = "CrucifixDropped";

    /// <summary>
    /// Virtual items spawned for held mobs.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> VirtualItems = new();
}
