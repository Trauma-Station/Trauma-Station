// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.CombatMode;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Melee;
using Content.Trauma.Shared.Knowledge.Miscellanious.Components;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Knowledge.Miscellanious.Systems;

public sealed partial class QueuedStrikeSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedCombatModeSystem _combat = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<QueuedStrikeComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            if (curTime < comp.TimeToHit)
                continue;

            var weapon = _hands.GetActiveItemOrSelf(ent);
            if (!TryComp<MeleeWeaponComponent>(weapon, out var meleeWeapon))
            {
                RemComp<QueuedStrikeComponent>(ent);
                continue;
            }
            var cachedTime = meleeWeapon.NextAttack;
            meleeWeapon.NextAttack = TimeSpan.Zero;
            var combat = _combat.IsInCombatMode(ent);
            _combat.SetInCombatMode(ent, true);
            _melee.AttemptLightAttack(ent, weapon, meleeWeapon, comp.Target);
            meleeWeapon.NextAttack = cachedTime;
            _combat.SetInCombatMode(ent, combat);
            RemComp<QueuedStrikeComponent>(ent);
        }
    }
}
