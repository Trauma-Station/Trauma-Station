// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Actions;

namespace Content.Medical.Shared.Abilities.Goliath;

public sealed class GoliathTentacleSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GoliathTentacleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GoliathTentacleComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(Entity<GoliathTentacleComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
        Dirty(ent);
    }

    private void OnShutdown(Entity<GoliathTentacleComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }
}
