// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.Tag;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Rust;

[RegisterComponent, NetworkedComponent]
public sealed partial class RustChargeComponent : Component
{
    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict =
        {
            {"Blunt", 50f},
        },
    };

    [DataField]
    public ProtoId<TagPrototype> IgnoreTag = "IgnoreImmovableRod";

    [DataField]
    public SoundSpecifier HitSound = new SoundCollectionSpecifier("MetalSlam");

    [DataField]
    public List<EntityUid> DamagedEntities = new();

    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(2);

    [DataField]
    public bool HadAoeRust;
}
