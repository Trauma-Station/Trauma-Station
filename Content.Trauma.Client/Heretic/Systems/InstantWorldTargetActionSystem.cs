// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Actions;
using Content.Shared.Actions.Components;
using Content.Trauma.Common.Heretic;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Heretic.Systems;

namespace Content.Trauma.Client.Heretic.Systems;

public sealed class InstantWorldTargetActionSystem : SharedInstantWorldTargetActionSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionComponent, TryPerformInstantWorldTargetActionEvent>(OnTryPerform);
    }

    private void OnTryPerform(Entity<ActionComponent> ent, ref TryPerformInstantWorldTargetActionEvent args)
    {
        if (TryComp(ent, out WorldTargetActionComponent? comp) && comp.Event is InstantWorldTargetActionEvent)
            _actions.TriggerAction(ent, true);
    }
}
