// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.ReactiveArmour;

/// <summary>
/// Checks if enought time have passed to activate reactive armour behavior
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ReactiveArmourComponent : Component
{
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    [DataField(required: true)]
    public TimeSpan ActivationDelay;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
    public TimeSpan LastActivated = default;
}
