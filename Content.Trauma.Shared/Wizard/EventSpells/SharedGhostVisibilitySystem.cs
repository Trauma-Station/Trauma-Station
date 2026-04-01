// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.GameTicking.Components;
using Content.Shared.Ghost;
using Content.Trauma.Common.Wizard;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Wizard.EventSpells;

public abstract class SharedGhostVisibilitySystem : CommonGhostVisibilitySystem
{
    protected static readonly EntProtoId GameRule = "GhostsVisible";

    public override bool IsVisible(EntityUid uid)
    {
        if (!GhostsVisible() || !TryComp<GhostComponent>(uid, out var component))
            return false;

        return !component.CanGhostInteract;
    }

    public override bool GhostsVisible()
    {
        var query = EntityQueryEnumerator<GhostsVisibleRuleComponent, ActiveGameRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out _, out _, out _, out _))
        {
            return true;
        }

        return false;
    }
}
