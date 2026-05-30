// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.EntityEffects;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Vampires.Umbrae;

/// <summary>
/// Action component that summons a shadow anchor at your location.
/// Recasting will teleport the target to the anchor.
///
/// After a certain amount of time, a shadow clone will be spawned at the anchor,
/// and the user becomes invisible for some seconds.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ActionShadowAnchorComponent : Component
{
    /// <summary>
    /// The anchor that we spawned
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Anchor;

    /// <summary>
    /// When to start the fake recall.
    /// </summary>
    [DataField]
    public TimeSpan FakeRecallDuration = TimeSpan.FromSeconds(5f);

    /// <summary>
    /// Effects that get applied to the user on fake recall.
    /// </summary>
    [DataField]
    public EntityEffect[] EffectsOnFakeRecall = default!;

    /// <summary>
    /// Has this action been cast?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Casted;
}

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ActiveActionShadowAnchorComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan FakeRecallUpdate;
}

public sealed partial class ShadowAnchorActionEvent : InstantActionEvent;
