// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Guardian;
using Content.Shared.Guardian.Components;

namespace Content.Goobstation.Shared.Guardian;

public sealed partial class GoobGuardianSystem : EntitySystem
{
    [Dependency] private GuardianSystem _guardian = default!;

    [SubscribeLocalEvent]
    private void OnPerformSelfAction(Entity<GuardianComponent> ent, ref GuardianToggleSelfActionEvent args)
    {
        if (ent.Comp.Host is { } host && TryComp<GuardianHostComponent>(host, out var hostComp) && ent.Comp.GuardianLoose)
            _guardian.ToggleGuardian((host, hostComp));

        args.Handled = true;
    }
}
