// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Antag;
using Content.Shared.Antag.Components;
using Content.Shared.Mind;
using Content.Trauma.Server.BloodCult.Gamerule;
using Content.Trauma.Shared.BloodCult;

namespace Content.Trauma.Server.BloodCult;

public sealed partial class ServerBloodCultSystem : BloodCultSystem
{
    [Dependency] private BloodCultRuleSystem _rule = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    private static readonly ProtoId<AntagSpecifierPrototype> CultistSpecifier = "BloodCultist";
    private static readonly ProtoId<AntagSpecifierPrototype> ConstructSpecifier = "BloodCultConstruct";

    public override void Convert(EntityUid rule, EntityUid target)
        => _rule.Convert(rule, target, CultistSpecifier);

    public override void ConvertConstruct(EntityUid rule, EntityUid target)
        => _rule.Convert(rule, target, ConstructSpecifier);

    public override void DeconvertConstruct(EntityUid target)
    {
        if (GetRule(target) is not { } rule)
            return;

        _mind.ClearObjectives(target);
        RemoveAntag(rule, target, ConstructSpecifier);
        RemComp<BloodCultMemberComponent>(target);
        // role should be removed separately...
    }

    // awesome antag selection API btw
    private void RemoveAntag(EntityUid rule, EntityUid target, [ForbidLiteral] ProtoId<AntagSpecifierPrototype> id)
    {
        if (_mind.GetMind(target) is not { } mind ||
            !TryComp<AntagSelectionComponent>(rule, out var antag) ||
            !antag.AssignedMinds.TryGetValue(id, out var minds))
            return;

        minds.RemoveWhere(pair => pair.Item1 == mind);
    }
}
