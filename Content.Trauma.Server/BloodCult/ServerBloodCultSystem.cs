// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Antag;
using Content.Shared.Antag.Components;
using Content.Shared.Mind;
using Content.Trauma.Server.BloodCult.Gamerule;
using Content.Trauma.Shared.BloodCult;
using Content.Trauma.Shared.Roles;

namespace Content.Trauma.Server.BloodCult;

public sealed partial class ServerBloodCultSystem : BloodCultSystem
{
    [Dependency] private BloodCultRuleSystem _rule = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    private static readonly ProtoId<AntagSpecifierPrototype> LeaderSoecifier = "BloodCultLeader";
    private static readonly ProtoId<AntagSpecifierPrototype> CultistSpecifier = "BloodCultist";
    private static readonly ProtoId<AntagSpecifierPrototype> ConstructSpecifier = "BloodCultConstruct";

    public override bool Convert(EntityUid rule, EntityUid target)
        => _rule.Convert(rule, target, CultistSpecifier);

    public override void ConvertConstruct(EntityUid rule, EntityUid target)
        => _rule.Convert(rule, target, ConstructSpecifier);

    public override void DeconvertConstruct(EntityUid target)
    {
        if (GetRule(target) is not { } rule)
            return;

        RemoveAntag(rule, target, ConstructSpecifier);
        RemComp<BloodCultMemberComponent>(target);
        // role should be removed separately...
    }

    public override void Deconvert(EntityUid target)
    {
        if (!_mind.TryGetMind(target, out var mindId, out var mind))
            return;

        if (MindGetRule(mindId) is { } rule)
        {
            RemoveAntag(rule, target, CultistSpecifier);
            RemoveAntag(rule, target, LeaderSoecifier);
        }

        _mind.ClearObjectives((mindId, mind));
        Role.MindRemoveRole<BloodCultistRoleComponent>((mindId, mind));
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
