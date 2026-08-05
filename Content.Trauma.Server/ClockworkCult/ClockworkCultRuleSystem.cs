// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Antag;
using Content.Server.GameTicking.Rules;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mind;
using Content.Shared.Mindshield;
using Content.Shared.Roles;
using Content.Shared.Speech.Components;
using Content.Trauma.Server.ClockworkCult.Components;
using Content.Trauma.Shared.ClockworkCult.Components;
using Content.Trauma.Shared.Roles;

namespace Content.Trauma.Server.ClockworkCult;

public sealed partial class ClockworkCultRuleSystem : GameRuleSystem<ClockworkCultRuleComponent>
{
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MindShieldSystem _mindShield = default!;
    [Dependency] private SharedRoleSystem _role = default!;

    private static readonly EntProtoId MindRole = "MindRoleClockworkCult";
    private static readonly EntProtoId Slab = "BibleSlabClockwork";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClockworkCultRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagSelected);
        SubscribeLocalEvent<ClockworkCultComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnAntagSelected(Entity<ClockworkCultRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        AddCultist(ent, args.EntityUid, conversion: false);
    }

    private void OnShutdown(Entity<ClockworkCultComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (AssociatedGamerule(ent) is not { } rule)
            return;

        rule.Comp.Cultists.Remove(ent);
    }

    private void AddCultist(Entity<ClockworkCultRuleComponent> rule, EntityUid uid, bool conversion)
    {
        var cult = EnsureComp<ClockworkCultComponent>(uid);
        EnsureComp<RatvarianLanguageComponent>(uid);

        var associated = EnsureComp<ClockworkCultAssociatedRuleComponent>(uid);
        associated.Rule = rule;

        rule.Comp.Cultists.Add(uid);

        if (!conversion)
        {
            _antag.SendBriefing(uid,
                Loc.GetString("clockworkcult-role-greeting"),
                Color.FromHex("#c98a34"),
                rule.Comp.StartSound);
            _antag.SendBriefing(uid,
                Loc.GetString("clockworkcult-role-greeting-short"),
                Color.FromHex("#d9bf8a"),
                null);

            var slab = Spawn(Slab, Transform(uid).Coordinates);
            _hands.TryPickupAnyHand(uid, slab);
        }
        else
        {
            _antag.SendBriefing(uid,
                Loc.GetString("clockworkcult-role-conversion"),
                Color.FromHex("#c98a34"),
                null);
        }

        Dirty(uid, cult);
        Dirty(rule);
    }

    public Entity<ClockworkCultRuleComponent>? AssociatedGamerule(EntityUid uid)
    {
        if (!TryComp<ClockworkCultAssociatedRuleComponent>(uid, out var associated))
            return null;

        return TryComp<ClockworkCultRuleComponent>(associated.Rule, out var rule)
            ? (associated.Rule, rule)
            : null;
    }

    public bool TryConvert(EntityUid converter, EntityUid target)
    {
        if (converter == target ||
            HasComp<ClockworkCultComponent>(target) ||
            _mindShield.IsShielded(target) ||
            AssociatedGamerule(converter) is not { } rule ||
            !_mind.TryGetMind(target, out var targetMindId, out var targetMind))
        {
            return false;
        }

        if (!_role.MindHasRole<ClockworkCultRoleComponent>(targetMindId))
            _role.MindAddRole(targetMindId, MindRole, targetMind, true);

        AddCultist(rule, target, conversion: true);
        return true;
    }
}
