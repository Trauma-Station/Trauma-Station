// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Arena;

/// <summary>
/// Action component that creates an area when an action is performed.
/// Must be a target action.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ActionArenaComponent : Component
{
    /// <summary>
    /// The wall prototype to use.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId WallProto;

    /// <summary>
    /// How big the arena will be.
    /// </summary>
    [DataField]
    public int ArenaSize = 3;

    /// <summary>
    /// If there should be a delay before making the arena.
    /// </summary>
    [DataField]
    public TimeSpan? Delay;

    /// <summary>
    /// Whether to predict the spawning of the <see cref="WallProto"/>.
    /// </summary>
    [DataField]
    public bool Predicted = true;

    /// <summary>
    /// The target entity that will be used for creating the arena around it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    /// <summary>
    /// Wall entities used to destroy the arena when needed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Walls = new();
}

/// <summary>
/// Active variant of <see cref="ActionArenaComponent"/>.
/// Gets applied if <see cref="ActionArenaComponent.Delay"/> is not null.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ActiveActionArenaComponent : Component
{
    /// <summary>
    /// Based on <see cref="ActionArenaComponent.Delay"/>.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextCheck;
}

public sealed partial class ArenaTargetActionEvent : EntityTargetActionEvent;
