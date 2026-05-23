// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Movement.Events;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;

namespace Content.Trauma.Shared.Xenomorphs.Stealth;

public sealed partial class StealthOnWalkSystem : EntitySystem
{
    [Dependency] private SharedStealthSystem _stealth = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StealthOnWalkComponent, SprintingInputEvent>(OnSprintingInput);
    }

    private void OnSprintingInput(EntityUid uid, StealthOnWalkComponent component, SprintingInputEvent args)
    {
        if (!TryComp<StealthComponent>(uid, out var stealth) || stealth.Enabled == !args.Entity.Comp.Sprinting)
            return;

        _stealth.SetEnabled(uid, !args.Entity.Comp.Sprinting, stealth);
        component.Stealth = stealth.Enabled;
    }
}
