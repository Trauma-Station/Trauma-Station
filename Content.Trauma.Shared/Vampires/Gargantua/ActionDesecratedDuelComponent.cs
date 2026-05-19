// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Vampires.Gargantua;

/// <summary>
/// Action component that initiates an area and a duel between two entities.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ActionDesecratedDuelComponent : Component
{
    /// <summary>
    /// How long the duel will last for.
    /// </summary>
    [DataField]
    public TimeSpan DuelDuration = TimeSpan.FromSeconds(10f);

    /// <summary>
    /// How often to check for fighters (if they died, or got deleted).
    /// </summary>
    [DataField]
    public TimeSpan FighterCheck = TimeSpan.FromSeconds(5f);

    /// <summary>
    /// The entity initiating the duel.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Duelist;

    /// <summary>
    /// The entity targeted by the duel.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Target;

    /// <summary>
    /// Effects that will run once the duel ends.
    /// </summary>
    [DataField]
    public EntityEffect[] EndEffects = default!;
}

/// <summary>
/// Active variant of the <see cref="ActionDesecratedDuelComponent"/> that gets applied when the action gets performed.
/// Removed when <see cref="DuelCheck"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ActiveActionDesecratedDuelComponent : Component
{
    /// <summary>
    /// Based on <see cref="ActionDesecratedDuelComponent.DuelDuration"/>.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan DuelCheck;

    /// <summary>
    /// Based on <see cref="ActionDesecratedDuelComponent.FighterCheck"/>.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextFighterCheck;
}
