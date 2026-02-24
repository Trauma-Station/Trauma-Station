using Content.Medical.Common.Body;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Body;

namespace Content.Medical.Shared.Body;

public sealed class OrganActionsSystem : EntitySystem
{
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    private EntityQuery<OrganComponent> _organQuery;

    public override void Initialize()
    {
        base.Initialize();

        _organQuery = GetEntityQuery<OrganComponent>();

        SubscribeLocalEvent<OrganActionsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<OrganActionsComponent, OrganEnabledEvent>(OnEnabled);
        SubscribeLocalEvent<OrganActionsComponent, OrganDisabledEvent>(OnDisabled);
    }

    private void OnMapInit(Entity<OrganActionsComponent> ent, ref MapInitEvent args)
    {
        var actions = EnsureComp<ActionsContainerComponent>(ent);
        foreach (var id in ent.Comp.Actions)
        {
            _actionContainer.AddAction(ent, id, actions);
        }
    }

    private void OnEnabled(Entity<OrganActionsComponent> ent, ref OrganEnabledEvent args)
    {
        var container = EnsureComp<ActionsContainerComponent>(ent);
        _actions.GrantContainedActions(args.Body, (ent, container));
    }

    private void OnDisabled(Entity<OrganActionsComponent> ent, ref OrganDisabledEvent args)
    {
        _actions.RemoveProvidedActions(args.Body, ent.Owner);
    }
}
