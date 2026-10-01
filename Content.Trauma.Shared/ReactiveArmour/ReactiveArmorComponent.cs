// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.ReactiveArmor;

/// <summary>
/// Checks if enought time have passed to activate reactive armour behavior
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ReactiveArmorComponent : Component
{
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    [DataField(required: true)]
    public EntityEffect[] EmpEffects = default!;


    [DataField]
    public TimeSpan ActivationDelay = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan EmpDuration = TimeSpan.FromSeconds(15);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
    public TimeSpan LastActivated = default;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
    public TimeSpan LastEmpd = default;


    [DataField]
    public string EmpMessage = "reactive-armor-emp-default";


    [DataField]
    public bool ApplyEmpEffectOnUser = true;

    [DataField]
    public bool ApplyOnEmpOnly = false;
}
