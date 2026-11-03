// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Megafauna;

/// <summary>
/// Component that runs effects once megafauna shutdowns
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MegafaunaShutdownEffectsComponent : Component
{
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;
}
