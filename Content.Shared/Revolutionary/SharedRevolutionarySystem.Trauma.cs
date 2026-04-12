// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Revolutionary.Components;
using Content.Trauma.Common.Mindshield;

namespace Content.Shared.Revolutionary;

/// <Trauma>
/// Trauma - goob rev mindshield conversion changes
/// </Trauma>
public abstract partial class SharedRevolutionarySystem
{
    /// <summary>
    /// Change headrevs ability to convert people
    /// </summary>
    public void SetConvertAbility(Entity<HeadRevolutionaryComponent> headRev, bool enabled = true)
    {
        headRev.Comp.ConvertAbilityEnabled = enabled;
    }

    private void OnMindshieldRemoval(Entity<HeadRevolutionaryComponent> headRev, ref RemoveMindShieldEvent args)
    {
        _popupSystem.PopupEntity(Loc.GetString("head-rev-break-mindshield"), headRev);
        SetConvertAbility(headRev, false); // GoobStation - turn off headrev ability to convert
        //QueueDel(implant); - Goobstation - Headrevs should remove implant before turning on ability
        args.Cancelled = true;
    }
}
