// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.Serialization;

namespace Content.Medical.Common.Damage;

[Serializable, NetSerializable]
public enum SplitDamageBehavior : byte
{
    None,
    Split,
    SplitEnsureAllOrganic,
    SplitEnsureAllDamaged,
    SplitEnsureAllDamagedAndOrganic,
    SplitEnsureAll
}
