// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Heretic.Components.StatusEffects;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class AddProtectiveBladesStatusEffectComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField]
    public TimeSpan Interval = TimeSpan.FromMilliseconds(500);

    [DataField]
    public int MaxBlades = 3;

    [DataField]
    public bool RefreshBlades;

    [DataField]
    public List<EntityUid> ActiveBlades = new();
}
