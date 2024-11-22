// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Server.BloodCult.Gamerule;
using Content.Trauma.Shared.BloodCult;

namespace Content.Trauma.Server.BloodCult;

public sealed partial class ServerBloodCultSystem : BloodCultSystem
{
    [Dependency] private BloodCultRuleSystem _rule = default!;

    public override EntityUid? GetTarget(EntityUid member)
        => _rule.GetRule(member)?.Comp.OfferingTarget;

    public override bool IsTarget(EntityUid member, EntityUid target)
        => GetTarget(member) == target;

    public override bool TargetKilled(EntityUid member)
        => _rule.GetRule(member)?.Comp.TargetSacrificed ?? false;

    public override void Convert(EntityUid member, EntityUid target)
        => _rule.Convert(member, target);
}
