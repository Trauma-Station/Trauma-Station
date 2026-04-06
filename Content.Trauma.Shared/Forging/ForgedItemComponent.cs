// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Forging;

/// <summary>
/// Component added to procedurally generated forged items.
/// Controls stuff like changing the name and stats.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ForgingSystem))]
[AutoGenerateComponentState]
public sealed partial class ForgedItemComponent : Component
{
    /// <summary>
    /// What this is meant to be in the end
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ForgedItemPrototype> Item;

    /// <summary>
    /// False for unfinished ingot being worked on, true after it is wrought.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Completed;
}

/// <summary>
/// Raised on a forged item after its item and metal prototypes have been set.
/// Happens for both unfinished and complete items, before stats are changed.
/// </summary>
[ByRefEvent]
public record struct ItemForgedEvent(ProtoId<ForgedItemPrototype> Item);

/// <summary>
/// Raised on a forged result after it has been wrought to change its stats.
/// If the user is non-null, also gets raised on it.
/// </summary>
[ByRefEvent]
public record struct ForgingCompletedEvent(MetalPrototype Metal, ForgedItemPrototype Item, EntityUid Target, EntityUid? User);
