// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Forging;

/// <summary>
/// Component for metallic items that can be struck with a hammer to turn into something else.
/// Striking it only does anything while the metal is workable, otherwise you just break it.
/// Requires <see cref="MetallicComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(WorkableSystem))]
[AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class WorkableComponent : Component
{
    /// <summary>
    /// The damage type to require for working.
    /// </summary>
    [DataField]
    public ProtoId<DamageTypePrototype> DamageType = "Blunt";

    /// <summary>
    /// How much damage is needed.
    /// Set when it is heated up to <see cref="IdealTemp"/>, unset when cooled below <see cref="MinTemp"/>.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public FixedPoint2 Remaining;

    /// <summary>
    /// The item to spawn and transfer heat to when <see cref="Remaining"/> reaches 0.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId Result;

    /// <summary>
    /// How many items to spawn.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Amount = 1;
}

/// <summary>
/// Raised on the original metal to transfer stats to the new result before being deleted.
/// </summary>
[ByRefEvent]
public record struct MetalWroughtEvent(EntityUid Result, EntityUid? User);
