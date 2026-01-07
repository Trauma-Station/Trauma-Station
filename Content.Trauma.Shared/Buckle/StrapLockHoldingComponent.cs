// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Trauma.Shared.EntityEffects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Buckle;

/// <summary>
/// Component given to someone actively holding someone up on a <see cref="StrapLockComponent"/>.
/// If they get too far away or drop the virtual items, the person gets dropped.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(StrapLockSystem))]
[AutoGenerateComponentState]
public sealed partial class StrapLockHoldingComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Strap;

    [DataField, AutoNetworkedField]
    public EntityUid Buckled;

    /// <summary>
    /// Once you leave this range you automatically drop the buckled person.
    /// </summary>
    [DataField]
    public float Range = 2f;

    /// <summary>
    /// The entity effect prototype to run on the buckled person if dropped.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<EntityEffectPrototype> DropEffect;
}
