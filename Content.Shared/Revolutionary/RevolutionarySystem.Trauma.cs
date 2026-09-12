// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Revolutionary.Components;

namespace Content.Shared.Revolutionary;

public sealed partial class RevolutionarySystem
{
    /// <summary>
    /// Change headrevs ability to convert people
    /// </summary>
    public void SetConvertAbility(Entity<HeadRevolutionaryComponent> ent, bool enabled = true)
    {
        if (ent.Comp.ConvertAbilityEnabled == enabled)
            return;

        ent.Comp.ConvertAbilityEnabled = enabled;
        Dirty(ent);
    }
}
