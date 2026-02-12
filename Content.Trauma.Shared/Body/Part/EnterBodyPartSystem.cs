// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Trauma.Common.Body.Part;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Body.Part;

public sealed class EnterBodyPartSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IngestionSystem _ingestion = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnterBodyPartComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EnterBodyPartComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<EnterBodyPartComponent, EnterBodyPartActionEvent>(OnAction);
        SubscribeLocalEvent<EnterBodyPartComponent, EnterBodyPartDoAfterEvent>(OnDoAfter);
    }

    private void OnMapInit(Entity<EnterBodyPartComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
        if (ent.Comp.ActionEntity is {} action)
            _actions.SetEntityIcon(action, ent);
    }

    private void OnShutdown(Entity<EnterBodyPartComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnAction(Entity<EnterBodyPartComponent> ent, ref EnterBodyPartActionEvent args)
    {
        args.Handled = true;
        var user = ent.Owner;
        var target = args.Target;
        var targetName = Identity.Entity(target, EntityManager);
        if (_mob.IsDead(target))
        {
            _popup.PopupClient(Loc.GetString("enter-body-part-dead", ("target", targetName)), user, user);
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager,
            ent,
            ent.Comp.Delay,
            new EnterBodyPartDoAfterEvent(),
            eventTarget: ent,
            target: target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            Hidden = true, // it's telegraphed by popup, dont need random people to see le valid doafter
        };
        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            return;

        _popup.PopupClient(Loc.GetString("enter-body-part-entering-user", ("target", targetName)), target, user);
        _popup.PopupEntity(Loc.GetString("enter-body-part-entering-target", ("user", ent.Owner)), user, target);
    }

    private void OnDoAfter(Entity<EnterBodyPartComponent> ent, ref EnterBodyPartDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not {} target)
            return;

        args.Handled = true;
        var user = ent.Owner;
        if (!_ingestion.HasMouthAvailable(user, target, SlotFlags.MASK | SlotFlags.HEAD))
            return; // it will do a popup internally

        var targetName = Identity.Entity(target, EntityManager);
        if (_body.GetOrgan(target, ent.Comp.Category) is not {} part)
        {
            var partName = _proto.Index(ent.Comp.Category).Name;
            _popup.PopupClient(Loc.GetString("enter-body-part-no-part", ("target", targetName), ("part", partName)), user, user);
            return; // nothing to enter
        }

        var ev = new GetBodyPartCavityEvent();
        RaiseLocalEvent(part, ref ev);
        if (ev.Container is not {} container)
        {
            Log.Error($"{ToPrettyString(ent)} tried to enter part {ToPrettyString(part)} of {ToPrettyString(target)} with no cavity");
            _popup.PopupClient(Loc.GetString("enter-body-part-no-cavity"), user, user);
            return; // shitcode
        }

        if (!_container.Insert(ent.Owner, container))
        {
            _popup.PopupClient(Loc.GetString("enter-body-part-cavity-full"), user, user);
            return;
        }

        var you = Loc.GetString("enter-body-part-entered-you", ("target", targetName));
        var others = Loc.GetString("enter-body-part-entered-others", ("user", user), ("target", targetName));
        _popup.PopupPredicted(you, others, target, user, PopupType.LargeCaution);
    }
}

[Serializable, NetSerializable]
public sealed partial class EnterBodyPartDoAfterEvent : SimpleDoAfterEvent;
