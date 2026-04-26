// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Common.Interaction;
using Content.Medical.Common.Surgery.Tools;
using Content.Medical.Shared.Surgery.Tools;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Medical.Shared.Surgery;

public sealed partial class SelectorSurgerySystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryToolComponent, ComponentInit>(OnToolStartup);
        SubscribeLocalEvent<SurgeryToolComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
        SubscribeLocalEvent<SurgeryToolComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<SurgeryToolComponent, UseInHandAttemptEvent>(UseInHand);
    }

    private void OnToolStartup(Entity<SurgeryToolComponent> ent, ref ComponentInit args)
    {
        CycleToolMode(ent, ent);
    }

    private void OnGetVerbs(Entity<SurgeryToolComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess) return;

        var user = args.User;
        var verb = new InteractionVerb
        {
            Act = () => CycleToolMode(ent, user),
            Text = "Switch Surgery Mode",
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/_Shitmed/Objects/Specific/Medical/Surgery/scalpel.rsi/scalpel.png")),
            Priority = 1
        };

        args.Verbs.Add(verb);
    }

    public void CycleToolMode(Entity<SurgeryToolComponent> ent, EntityUid user)
    {
        if (AllComps<ISurgeryToolComponent>(ent).ToList() is not { } comps || comps.Count < 1)
            return;

        ent.Comp.ActiveIndex = (ent.Comp.ActiveIndex + 1) % comps.Count;
        ent.Comp.ActiveSurgicalComp = comps[ent.Comp.ActiveIndex];

        if (comps.Count > 1)
            _popup.PopupClient(Loc.GetString("surgery-popup-cycle-tool", ("tool", Name(ent)), ("type", ent.Comp.ActiveSurgicalComp.ToolName)), user, user);
        Dirty(ent);
    }

    private void OnExamine(Entity<SurgeryToolComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || ent.Comp.ActiveSurgicalComp is not { } active)
            return;

        args.PushMarkup(Loc.GetString("surgery-tool-examine-mode", ("mode", active.ToolName)));
    }

    private void UseInHand(Entity<SurgeryToolComponent> ent, ref UseInHandAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        CycleToolMode(ent, args.User);
    }

    /*
    private void OnToolStep(Entity<SurgeryStepComponent> ent, ref SurgeryStepEvent args)
    {
        if (!TryToolAudio(ent, args))
            return;

        ApplyComponentChanges(args, ent.Comp);
        HandleOrganModifications(args, ent.Comp);

        HandleSanitization(args);
    }

    private void HandleSanitization(SurgeryStepEvent args)
    {
        if (_inventory.TryGetSlotEntity(args.User, "gloves", out var _)
            && _inventory.TryGetSlotEntity(args.User, "mask", out var _))
            return;

        var sepsisEv = new SurgerySanitizationEvent();
        RaiseLocalEvent(args.User, ref sepsisEv);
        if (sepsisEv.Handled)
            return;

        if (TryComp<SurgeryTargetComponent>(args.Body, out var surgeryTargetComponent) &&
            surgeryTargetComponent.SepsisImmune)
            return;

        var sepsis = new DamageSpecifier(_prototypes.Index(Poison), 5);
        var ev = new SurgeryStepDamageEvent(args.User, args.Body, args.Part, args.SurgeryId, sepsis, 0.5f);
        RaiseLocalEvent(args.Body, ref ev);
    }

    private bool TryToolAudio(Entity<SurgeryStepComponent> ent, SurgeryStepEvent args)
    {
        if (ent.Comp.Tool == null)
            return true;

        foreach (var reg in ent.Comp.Tool.Values)
        {
            if (!HasSurgeryComp(args.Tool, reg.Component))
                return false;

            if (_toolQuery.CompOrNull(args.Tool)?.EndSound is not { } sound)
                continue;
            _audio.PlayPredicted(sound, args.Tool, args.User);
            break; // no overlaying sounds
        }

        return true;
    }
    */
}
