// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffectOnUse;

/// <summary>
/// Applyes effects on use in hand.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EntityEffectOnUseComponent : Component
{
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    [DataField]
    public bool ApplyToUser = false;
}
