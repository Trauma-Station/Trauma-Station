// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Jittering;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Genetics.Tools;

public sealed class EnzymeIncubatorSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedJitteringSystem _jittering = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly UniqueEnzymesSystem _enzymes = default!;

    private static readonly ProtoId<TagPrototype> TrashTag = "Trash";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnzymeIncubatorComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<EnzymeIncubatorComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<EnzymeIncubatorComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<EnzymeIncubatorComponent, EnzymeIncubatorDoAfterEvent>(OnDoAfter);
    }

    private void OnExamined(Entity<EnzymeIncubatorComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var msg = ent.Comp.Enzymes?.Name is {} name
            ? Loc.GetString("enzyme-incubator-examine-loaded", ("name", name))
            : Loc.GetString("enzyme-incubator-examine-spent");
        args.PushMarkup(msg);
    }

    private void OnAfterInteract(Entity<EnzymeIncubatorComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not {} target)
            return;

        args.Handled = true;
        StartInject(ent, target, args.User);
    }

    private void OnUseInHand(Entity<EnzymeIncubatorComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        StartInject(ent, args.User, args.User);
    }

    public void StartInject(Entity<EnzymeIncubatorComponent> ent, EntityUid target, EntityUid user)
    {
        if (ent.Comp.Enzymes == null)
        {
            _popup.PopupClient(Loc.GetString("enzyme-incubator-depleted"), user, user);
            return;
        }

        var targetName = Identity.Name(target, EntityManager);
        if (!_mutation.CanMutate(target))
        {
            _popup.PopupClient(Loc.GetString("enzyme-incubator-cant-mutate", ("target", targetName)), user, user);
            return;
        }

        var userName = Identity.Name(user, EntityManager);
        var you = Loc.GetString("enzyme-incubator-mutating-you", ("user", userName), ("item", ent));
        var others = Loc.GetString("EnzymeIncubator-mutating-others", ("user", userName), ("target", targetName), ("item", ent));
        _popup.PopupPredicted(you, others, ent, target);

        // injecting someone else takes twice as long
        var delay = ent.Comp.Delay;
        if (user != target)
            delay *= 2;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user,
            delay,
            new EnzymeIncubatorDoAfterEvent(),
            eventTarget: ent,
            target: target,
            used: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true
        });
    }

    private void OnDoAfter(Entity<EnzymeIncubatorComponent> ent, ref EnzymeIncubatorDoAfterEvent args)
    {
        if (args.Cancelled ||
            args.Target is not {} target ||
            ent.Comp.Enzymes is not {} enzymes)
            return;

        args.Handled = true;

        _adminLog.Add(LogType.Genetics, LogImpact.High, $"{ToPrettyString(args.User)} used {ToPrettyString(ent)} to change {ToPrettyString(target)} to {enzymes.Name}!");

        _enzymes.ChangeEnzymes(target, enzymes);
        _jittering.DoJitter(target, ent.Comp.JitterTime, refresh: true);
        if (ent.Comp.Damage is {} damage)
            _damage.ChangeDamage(target, damage, ignoreResistances: true, origin: ent);

        // target's popup is more anti-shitter than anything, you can easily tell if someone else got mutated
        _popup.PopupClient(Loc.GetString("scramble-on-trigger-popup"), target, target);

        if (ent.Comp.Infinite)
            return; // :)

        // prevent reuse
        ent.Comp.Enzymes = null;
        Dirty(ent);
        UpdateAppearance(ent);
        // allow recycling in disposals
        _tag.AddTag(ent, TrashTag);
    }

    private void UpdateAppearance(Entity<EnzymeIncubatorComponent> ent)
    {
        _appearance.SetData(ent, MutatorVisuals.Spent, ent.Comp.Enzymes == null);
    }

    #region Public API

    public void SetEnzymes(Entity<EnzymeIncubatorComponent?> ent, UniqueEnzymes? enzymes)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Enzymes = enzymes;
        Dirty(ent, ent.Comp);
        UpdateAppearance((ent, ent));
    }

    #endregion
}

[Serializable, NetSerializable]
public sealed partial class EnzymeIncubatorDoAfterEvent : SimpleDoAfterEvent;
