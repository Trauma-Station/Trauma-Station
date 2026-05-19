// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Toggleable;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Ash;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Ash;

public sealed partial class ToggleActionSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToggleActionComponent, ToggleActionEvent>(OnToggle);
    }

    private void OnToggle(Entity<ToggleActionComponent> ent, ref ToggleActionEvent args)
    {
        _actions.SetToggled(args.Action.AsNullable(), !args.Action.Comp.Toggled);
    }
}
