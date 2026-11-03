// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Shared.BloodCult.Items;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.BloodCult.Spells;

public sealed partial class BloodCultTeleportSpellSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityQuery<CultItemComponent> _cultItemQuery = default!;

    [SubscribeLocalEvent]
    private void OnMeleeHit(Entity<BloodCultCombatMarkComponent> ent, ref AttackedEvent args)
    {
        if (!_cultItemQuery.HasComp(args.Used))
            return;

        _effects.TryApplyEffect(ent, ent.Comp.Effect, user: args.User);
    }

    [SubscribeLocalEvent]
    private void OnStunned(Entity<BloodCultCombatMarkComponent> ent, ref StunnedEvent args)
    {
        RemCompDeferred(ent, ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<BloodCultCombatMarkComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.DisappearTime = _timing.CurTime + ent.Comp.Duration;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var now = _timing.CurTime;

        var query = EntityQueryEnumerator<BloodCultCombatMarkComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.DisappearTime)
                continue;

            RemCompDeferred(uid, comp);
        }
    }
}
