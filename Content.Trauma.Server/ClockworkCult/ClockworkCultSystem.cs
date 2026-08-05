// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Actions;
using Content.Shared.Bible.Components;
using Content.Shared.DoAfter;
using Content.Shared.Mind;
using Content.Shared.Mindshield;
using Content.Shared.Popups;
using Content.Shared.Speech.Components;
using Content.Shared.Stunnable;
using Content.Trauma.Shared.ClockworkCult;
using Content.Trauma.Shared.ClockworkCult.Components;

namespace Content.Trauma.Server.ClockworkCult;

public sealed partial class ClockworkCultSystem : EntitySystem
{
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MindShieldSystem _mindShield = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ClockworkCultRuleSystem _rule = default!;
    [Dependency] private SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClockworkCultComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ClockworkCultComponent, EventClockworkConvert>(OnConvert);
        SubscribeLocalEvent<ClockworkCultComponent, ClockworkConvertDoAfterEvent>(OnConvertDoAfter);
    }

    private void OnInit(Entity<ClockworkCultComponent> ent, ref ComponentInit args)
    {
        EnsureComp<RatvarianLanguageComponent>(ent);
        ent.Comp.ConvertActionEntity = _actions.AddAction(ent, ent.Comp.ConvertAction);
        Dirty(ent);
    }

    private void OnConvert(Entity<ClockworkCultComponent> ent, ref EventClockworkConvert args)
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (target == ent.Owner ||
            HasComp<ClockworkCultComponent>(target) ||
            HasComp<BibleUserComponent>(target) ||
            _mindShield.IsShielded(target) ||
            !_mind.TryGetMind(target, out _, out _))
        {
            _popup.PopupEntity(Loc.GetString("clockworkcult-convert-invalid"), ent, ent);
            return;
        }

        args.Handled = true;

        _stun.TryAddParalyzeDuration(target, ent.Comp.ConversionDelay);

        var doAfter = new DoAfterArgs(EntityManager,
            ent,
            ent.Comp.ConversionDelay,
            new ClockworkConvertDoAfterEvent(),
            ent,
            target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnDropItem = false,
            BreakOnHandChange = false,
            DistanceThreshold = 2.0f,
            Hidden = false,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnConvertDoAfter(Entity<ClockworkCultComponent> ent, ref ClockworkConvertDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target is not { } target)
            return;

        args.Handled = true;

        if (!_rule.TryConvert(ent, target))
            _popup.PopupEntity(Loc.GetString("clockworkcult-convert-failed"), ent, ent);
    }
}
