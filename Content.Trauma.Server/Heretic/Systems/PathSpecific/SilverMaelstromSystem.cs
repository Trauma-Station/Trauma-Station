// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Blade;

namespace Content.Trauma.Server.Heretic.Systems.PathSpecific;

public sealed class SilverMaelstromSystem : EntitySystem
{
    [Dependency] private readonly ProtectiveBladeSystem _pblade = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Shared.Heretic.Components.PathSpecific.Blade.SilverMaelstromComponent, ProtectiveBladeUsedEvent>(OnBladeUsed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var eqe = EntityQueryEnumerator<Shared.Heretic.Components.PathSpecific.Blade.SilverMaelstromComponent>();
        while (eqe.MoveNext(out var uid, out var smc))
        {
            if (!uid.IsValid())
                continue;

            smc.RespawnTimer -= frameTime; // TODO timespan

            if (smc.RespawnTimer <= 0)
            {
                smc.RespawnTimer = smc.RespawnCooldown;

                if (smc.ActiveBlades.Count < smc.MaxBlades)
                {
                    var blade = _pblade.AddProtectiveBlade(uid);
                    smc.ActiveBlades.Add(blade);
                }
            }
        }
    }

    private void OnBladeUsed(Entity<Shared.Heretic.Components.PathSpecific.Blade.SilverMaelstromComponent> ent, ref ProtectiveBladeUsedEvent args)
    {
        ent.Comp.ActiveBlades.Remove(args.Used);
        ent.Comp.ActiveBlades = ent.Comp.ActiveBlades.Where(Exists).ToList();
    }
}
