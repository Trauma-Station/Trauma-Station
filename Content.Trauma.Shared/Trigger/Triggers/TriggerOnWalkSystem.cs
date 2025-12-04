// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Trauma.Common.Movement;
using Content.Shared.Trigger.Systems;

namespace Content.Trauma.Shared.Trigger.Triggers;

public sealed class TriggerOnWalkSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnWalkComponent, FootStepEvent>(OnFootStep);
    }

    private void OnFootStep(Entity<TriggerOnWalkComponent> ent, ref FootStepEvent args)
    {
        _trigger.Trigger(ent, args.Mob, ent.Comp.KeyOut);
    }
}
