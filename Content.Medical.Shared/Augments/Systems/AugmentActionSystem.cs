using Content.Medical.Common.Body;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Toggleable;

namespace Content.Medical.Shared.Augments;

public sealed class AugmentActionSystem : EntitySystem
{
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AugmentActionComponent, OrganEnabledEvent>(OnOrganEnabled);
        SubscribeLocalEvent<AugmentActionComponent, OrganDisabledEvent>(OnOrganDisabled);
        SubscribeLocalEvent<AugmentActionComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<AugmentActionComponent, ToggleActionEvent>(OnToggleAction);
    }

    private void OnOrganEnabled(Entity<AugmentActionComponent> ent, ref OrganEnabledEvent args)
    {
        EnsureComp<ActionsContainerComponent>(ent);
        _actions.AddAction(args.Body, ref ent.Comp.ActionEntity, ent.Comp.Action, ent);
    }

    private void OnOrganDisabled(Entity<AugmentActionComponent> ent, ref OrganDisabledEvent args)
    {
        _actions.SetToggled(ent.Comp.ActionEntity, false);
        _actions.RemoveAction(args.Body, ent.Comp.ActionEntity);
    }

    private void OnToggled(Entity<AugmentActionComponent> ent, ref ItemToggledEvent args)
    {
        _actions.SetToggled(ent.Comp.ActionEntity, args.Activated);
    }

    private void OnToggleAction(Entity<AugmentActionComponent> ent, ref ToggleActionEvent args)
    {
        _toggle.Toggle(ent.Owner, user: args.Performer);
        args.Handled = true;
    }
}
