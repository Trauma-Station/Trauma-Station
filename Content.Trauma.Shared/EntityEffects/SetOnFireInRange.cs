// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects.Effects;

/// <summary>
/// Sets on fire all entitys in set range.
/// </summary>
public sealed partial class SetOnFireInRange : EntityEffectBase<SetOnFireInRange>
{
    [DataField]
    public float SeachRange = 1.0f;

    [DataField(required: true)]
    public float FireStacks = default!;
}
