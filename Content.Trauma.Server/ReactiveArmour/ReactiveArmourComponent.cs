// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;


namespace Content.Trauma.Server.ReactiveArmour;

/// <summary>
/// Checks if enought time have passed
/// </summary>
[RegisterComponent]
public sealed partial class ReactiveArmourComponent : Component
{
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    [DataField]
    public TimeSpan ActivationDelay = TimeSpan.FromSeconds(0);

    public TimeSpan LastActivated = TimeSpan.FromSeconds(0);
}
