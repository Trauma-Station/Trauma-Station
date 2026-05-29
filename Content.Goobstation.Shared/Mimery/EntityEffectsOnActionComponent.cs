// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Goobstation.Shared.Mimery;

[RegisterComponent, NetworkedComponent]
public sealed partial class EntityEffectOnActionComponent : Component
{
    [DataField]
    public float Scale = 1f;

    [DataField(required: true)]
    public EntityEffect[] Effects;
}
