// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Wizard;

namespace Content.Shared.Temperature.Systems;

/// <summary>
/// Trauma - shitcode collection
/// </summary>
public abstract partial class SharedTemperatureSystem
{
    [Dependency] private CommonSpellbladeSystem _spellblade = default!;

    protected bool CanTakeHeatDamage(EntityUid uid)
        => !_spellblade.IsHoldingItemWithFireSpellbladeEnchantmentComponent(uid);
}
