using Content.Shared.CombatMode;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;
using Content.Trauma.Shared.Knowledge.Miscellanious.Components;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Knowledge.Miscellanious.Systems;

public sealed partial class QueuedParrySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<QueuedParryComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            if (curTime < comp.TimeToHit)
                continue;

            var weapon = _hands.GetActiveItemOrSelf(ent);
            if (!TryComp<MeleeWeaponComponent>(weapon, out var meleeWeapon))
            {
                RemComp<QueuedParryComponent>(ent);
                continue;
            }
            var cachedTime = meleeWeapon.NextAttack;
            meleeWeapon.NextAttack = TimeSpan.Zero;
            var combat = _combat.IsInCombatMode(ent);
            _combat.SetInCombatMode(ent, true);
            _melee.AttemptLightAttack(ent, weapon, meleeWeapon, comp.Target);
            meleeWeapon.NextAttack = cachedTime;
            _combat.SetInCombatMode(ent, combat);
            RemComp<QueuedParryComponent>(ent);
        }
    }
}
