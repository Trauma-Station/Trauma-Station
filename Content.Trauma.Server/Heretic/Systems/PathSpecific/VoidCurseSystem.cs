// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Temperature.Systems;
using Content.Shared.Atmos;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffect;
using Content.Shared.Temperature.Components;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Void;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Void;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Heretic.Systems.PathSpecific;

public sealed class VoidCurseSystem : SharedVoidCurseSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TemperatureSystem _temp = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var eqe = EntityQueryEnumerator<VoidCurseComponent>();
        while (eqe.MoveNext(out var uid, out var comp))
        {
            if (comp.Lifetime <= 0f)
            {
                if (comp.Stacks <= 1f)
                    RemCompDeferred(uid, comp);
                else
                {
                    comp.Stacks -= 1f;
                    RefreshLifetime(comp);
                    Dirty(uid, comp);
                }

                continue;
            }

            if (comp.NextUpdate > now)
                continue;

            comp.NextUpdate = now + comp.Timer;

            comp.Lifetime -= (float) comp.Timer.TotalSeconds;

            Cycle((uid, comp));
        }
    }

    private void Cycle(Entity<VoidCurseComponent> ent)
    {
        if (TryComp<TemperatureComponent>(ent, out var temp))
        {
            // temperaturesystem is not idiotproof :(
            var t = temp.CurrentTemperature - 3f * ent.Comp.Stacks;
            _temp.ForceChangeTemperature(ent, Math.Clamp(t, Atmospherics.TCMB, Atmospherics.Tmax), temp);
        }

        _statusEffect.TryAddStatusEffect<MutedComponent>(ent, "Muted", TimeSpan.FromSeconds(5), true);
    }
}
