// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Events;

namespace Content.Trauma.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem
{
    [SubscribeLocalEvent]
    private void OnDraftsModify(Entity<HereticComponent> ent, ref HereticModifySideKnowledgeDraftsEvent args)
    {
        foreach (var (key, value) in args.SideKnowledgeDrafts)
        {
            if (ent.Comp.SideKnowledgeDrafts.TryGetValue(key, out var existing))
                ent.Comp.SideKnowledgeDrafts[key] = Math.Max(0, existing + value);
            else
                ent.Comp.SideKnowledgeDrafts.Add(key, Math.Max(0, value));
        }
    }
}
