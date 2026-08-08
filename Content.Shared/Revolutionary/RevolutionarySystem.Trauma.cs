// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Revolutionary.Components;
using Content.Trauma.Common.Mindshield;

namespace Content.Shared.Revolutionary;

public sealed partial class RevolutionarySystem
{
    /// <summary>
    /// Change headrevs ability to convert people
    /// </summary>
    public void SetConvertAbility(Entity<HeadRevolutionaryComponent> headRev, bool enabled = true)
    {
        headRev.Comp.ConvertAbilityEnabled = enabled;
    }

    [SubscribeLocalEvent]
    private void OnHeadMindShieldAttempt(Entity<HeadRevolutionaryComponent> ent, ref MindShieldAttemptEvent args)
    {
        SetConvertAbility(ent, false);
        args.CancelPopup = "head-rev-break-mindshield";
    }
}
