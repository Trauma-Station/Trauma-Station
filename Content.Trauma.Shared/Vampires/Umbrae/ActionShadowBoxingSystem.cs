// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Vampires.Umbrae;

public sealed partial class ActionShadowBoxingSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private MobStateSystem _mob = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionShadowBoxingComponent, ShadowBoxingActionEvent>(OnShadowBox);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var eqe = EntityQueryEnumerator<ActiveActionShadowBoxingComponent, ActionShadowBoxingComponent>();
        while (eqe.MoveNext(out var uid, out var active, out var comp))
        {
            if (now < active.NextUpdate)
                continue;

            active.NextUpdate = now + comp.Update;
            Dirty(uid, active);

            var target = active.Target;
            var user = active.User;

            if (_mob.IsAlive(target) && _examine.InRangeUnOccluded(user, target, comp.RangeRequired))
            {
                _effects.ApplyEffects(target, comp.TargetEffects);
                continue;
            }

            RemCompDeferred(uid, active);
        }
    }

    private void OnShadowBox(Entity<ActionShadowBoxingComponent> ent, ref ShadowBoxingActionEvent args)
    {
        var comp = EnsureComp<ActiveActionShadowBoxingComponent>(ent.Owner);
        comp.NextUpdate = _timing.CurTime + ent.Comp.Update;
        comp.Target = args.Target;
        comp.User = args.Performer;

        Dirty(ent);
        args.Handled = true;
    }
}
