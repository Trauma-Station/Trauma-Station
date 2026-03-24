// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Temperature.Systems;
using Content.Shared.Atmos;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffect;
using Content.Shared.Temperature.Components;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Void;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Void;

namespace Content.Trauma.Server.Heretic.Systems.PathSpecific;

public sealed class VoidCurseSystem : SharedVoidCurseSystem
{
    [Dependency] private readonly TemperatureSystem _temp = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var eqe = EntityQueryEnumerator<VoidCurseComponent>();
        while (eqe.MoveNext(out var uid, out var comp))
        {
            if (comp.Lifetime <= 0)
            {
                if (comp.Stacks <= 1)
                    RemCompDeferred(uid, comp);
                else
                {
                    comp.Stacks -= 1;
                    RefreshLifetime(comp);
                    Dirty(uid, comp);
                }

                continue;
            }

            comp.Timer -= frameTime; // TODO: TimeSpan
            if (comp.Timer > 0)
                continue;

            comp.Timer = 1f;
            comp.Lifetime -= 1f;

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
