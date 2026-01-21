// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Actions;
using Content.Shared.Bed.Sleep;
using Content.Shared.Medical.Cryogenics;

namespace Content.Trauma.Shared.Medical;

/// <summary>
/// Lets you sleep inside cryo pods.
/// Being ejected from a cryo pod automatically tries to wake you up.
/// </summary>
public sealed class CryoPodSleepingSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SleepingSystem _sleeping = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InsideCryoPodComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<InsideCryoPodComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(Entity<InsideCryoPodComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.SleepAction, SleepingSystem.SleepActionId);
        Dirty(ent);
    }

    private void OnShutdown(Entity<InsideCryoPodComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.SleepAction);
        _sleeping.TryWaking(ent.Owner);
    }
}
