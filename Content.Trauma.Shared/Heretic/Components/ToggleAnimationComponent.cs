// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class ToggleAnimationComponent : Component
{
    [DataField]
    public TimeSpan ToggleOnTime = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan ToggleOffTime = TimeSpan.FromSeconds(1.6);

    [DataField]
    public ToggleAnimationState CurState;

    [DataField]
    public ToggleAnimationState NextState;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan ToggleEndTime = TimeSpan.Zero;
}

[Serializable, NetSerializable]
public enum ToggleAnimationVisuals : byte
{
    ToggleState,
}

[Serializable, NetSerializable]
public enum ToggleAnimationState : byte
{
    Off,
    TogglingOn,
    On,
    TogglingOff,
}
