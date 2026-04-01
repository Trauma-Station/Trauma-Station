// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Wizard;

public abstract partial class CommonSpellbladeSystem : EntitySystem
{
    /// <summary>
    /// Speicific API that gets if a player is holding an item with the fire spellblade enchantment component.
    /// </summary>
    public abstract bool IsHoldingItemWithFireSpellbladeEnchantmentComponent(EntityUid user);
}
