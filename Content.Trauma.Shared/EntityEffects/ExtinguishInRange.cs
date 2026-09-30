// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects.Effects;

/// <summary>
/// Extinguishes all entitys in set range.
/// </summary>
public sealed partial class ExtinguishInRange : EntityEffectBase<ExtinguishInRange>
{
    [DataField]
    public float SeachRange = 1.0f;
}