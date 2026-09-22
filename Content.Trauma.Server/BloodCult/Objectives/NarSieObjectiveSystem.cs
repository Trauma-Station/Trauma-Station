// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Objectives.Components;
using Content.Trauma.Server.BloodCult.Gamerule;

namespace Content.Trauma.Server.BloodCult.Objectives;

public sealed partial class NarSieObjectiveSystem : EntitySystem
{
    [Dependency] private BloodCultRuleSystem _rule = default!;

    [SubscribeLocalEvent]
    private void OnGetProgress(Entity<NarSieObjectiveComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        // TODO: store the rule on the mind role
        args.Progress = args.Mind.OwnedEntity is {} member && _rule.GetRule(member)?.Comp.NarSieSummoned ?? false ? 1f : 0f;
    }
}
