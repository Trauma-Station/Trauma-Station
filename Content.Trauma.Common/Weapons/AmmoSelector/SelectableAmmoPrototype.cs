// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Trauma.Common.Weapons.AmmoSelector;

[Prototype]
public sealed partial class SelectableAmmoPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;

    [DataField(required: true)]
    public string Desc = default!;

    [DataField(required: true)]
    public string ProtoId = default!; // this has to be a string because of how hitscan projectiles work

    [DataField]
    public Color? Color;

    [DataField]
    public float FireCost = 100f;

    [DataField]
    public SoundSpecifier? SoundGunshot;

    [DataField]
    public float FireRate = 8f;

    [DataField]
    public SelectableAmmoFlags Flags = SelectableAmmoFlags.ChangeWeaponFireCost;
}

[Serializable, NetSerializable]
public enum SelectableAmmoFlags : byte
{
    None = 0,
    ChangeWeaponFireCost = 1 << 0,
    ChangeWeaponFireSound = 1 << 1,
    ChangeWeaponFireRate = 1 << 2,
    All = ChangeWeaponFireCost | ChangeWeaponFireSound | ChangeWeaponFireRate,
}
