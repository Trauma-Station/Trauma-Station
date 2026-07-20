// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Tackle;

/// <summary>
/// This component doesn't make user tackle by default, they still need special equipment.
/// Unless they also have <see cref="TackleModifierComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class TacklerComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextTackle;

    [DataField, AutoNetworkedField]
    public TimeSpan TackleCooldown = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Setting this to false won't knockdown user on innate tackles
    /// If you want to disable knockdown for all tackles, set <see cref="KnockdownTime"/> to 0
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool KnockdownUser = true;

    [DataField, AutoNetworkedField]
    public float Range = 3f;

    [DataField, AutoNetworkedField]
    public float Speed = 6.5f;

    [DataField, AutoNetworkedField]
    public float MinDistance;

    [DataField, AutoNetworkedField]
    public float StaminaCost = 25f;

    [DataField, AutoNetworkedField]
    public float SkillMod;
}
