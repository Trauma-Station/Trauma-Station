// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Stunnable;
using Content.Shared.Damage.Systems;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Stunnable;

public sealed partial class OvertimeStaminaDamageSystem : EntitySystem
{
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OvertimeStaminaDamageComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<OvertimeStaminaDamageComponent> ent, ref ComponentInit args)
    {
        // TODO: an iq too high?
        // UNDER NO CIRCUMSTANCES ALLOW THIS SHIT TO RUN ON CLIENT
        if (_net.IsClient)
        {
            RemComp<OvertimeStaminaDamageComponent>(ent);
            return;
        }

        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.Delay;
        ent.Comp.Damage = ent.Comp.Amount;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<OvertimeStaminaDamageComponent>();
        var now = _timing.CurTime;
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextUpdate)
                continue;

            Update((uid, comp));
            comp.NextUpdate = _timing.CurTime + comp.Delay;
        }
    }

    private void Update(Entity<OvertimeStaminaDamageComponent> ent)
    {
        var damage = ent.Comp.Amount / ent.Comp.Delta;

        _stamina.TakeStaminaDamage(ent, damage, immediate: false, visual: false);

        ent.Comp.Damage -= damage;

        if (ent.Comp.Damage <= 0)
            RemComp(ent, ent.Comp);
    }
}
