// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.EntityEffects;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Vampires.Umbrae;

/// <summary>
/// Action component that spawns shadow clones at your location, and makes them target your selected target.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActionShadowBoxingComponent : Component
{
    /// <summary>
    /// The range required for the effects to take place on the target
    /// </summary>
    [DataField]
    public int RangeRequired = 2;

    /// <summary>
    /// Effects applied on the target.
    /// </summary>
    [DataField]
    public EntityEffect[] TargetEffects = default!;

    /// <summary>
    /// How often to update the effects
    /// </summary>
    [DataField]
    public TimeSpan Update = TimeSpan.FromSeconds(0.5f);
}

/// <summary>
/// Active variant that iterates over the victim and applies bad effects, depending on the range of the action user.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ActiveActionShadowBoxingComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate;

    /// <summary>
    /// The target of the action
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Target;

    /// <summary>
    /// The user of the action
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid User;
}


public sealed partial class ShadowBoxingActionEvent : EntityTargetActionEvent;
