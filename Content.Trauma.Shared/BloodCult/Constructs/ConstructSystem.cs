// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.BloodCult.Gamerule;

namespace Content.Trauma.Shared.BloodCult.Constructs;

/// <summary>
/// Tracks constructs in the blood cult gamerule.
/// </summary>
public sealed partial class ConstructSystem : EntitySystem
{
    [Dependency] private BloodCultSystem _cult = default!;

    [SubscribeLocalEvent]
    private void OnCultAssigned(Entity<ConstructComponent> ent, ref CultAssignedEvent args)
    {
        var rule = args.Rule;
        rule.Comp.Constructs.Add(ent);
        DirtyField(rule, rule.Comp, nameof(BloodCultRuleComponent.Constructs));
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<ConstructComponent> ent, ref ComponentShutdown args)
    {
        if (_cult.GetRule(ent) is not { } rule)
            return;

        rule.Comp.Constructs.Remove(ent);
        DirtyField(rule, rule.Comp, nameof(BloodCultRuleComponent.Constructs));
    }
}
