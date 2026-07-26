// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Tag;

namespace Content.Trauma.Shared.AER;

[RegisterComponent]
//component for spending projectile on hitting targets with the corresponding tag
public sealed partial class SpendProjectileOnTagComponent : Component
{
    [DataField]
    public ProtoId<TagPrototype> Tag = "Wall";
}
