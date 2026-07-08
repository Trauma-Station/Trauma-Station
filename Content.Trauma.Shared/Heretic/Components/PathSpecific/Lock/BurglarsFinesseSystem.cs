// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.CombatMode;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Trauma.Shared.Heretic.Systems;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Lock;

public sealed partial class BurglarsFinesseSystem : EntitySystem
{
    [Dependency] private SharedHereticSystem _heretic = default!;
    [Dependency] private SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private SharedCombatModeSystem _combat = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandsComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerb);
    }

    private void OnGetAltVerb(Entity<HandsComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;
        if (user == ent.Owner || args.Using is not { } used)
            return;

        if (!CanSteal(user, used))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Priority = 9,
            Act = () =>
            {
                if (!CanSteal(user, used)) // check again
                    return;

                DoSteal(user, used, ent);
            },
        });
    }

    private bool CanSteal(EntityUid user, EntityUid used)
    {
        return _heretic.TryGetHereticComponent(user, out var heretic, out _) &&
               heretic is { CurrentPath: HereticPath.Lock, PathStage: >= 6 } && HasComp<HereticBladeComponent>(used) &&
               _combat.IsInCombatMode(user);
    }

    private void DoSteal(EntityUid user, EntityUid used, Entity<HandsComponent> target)
    {
        if (!TryComp(used, out MeleeWeaponComponent? melee))
            return;

        if (!_melee.AttemptLightAttack(user, used, melee, target, false))
            return;

        melee.NextAttack += TimeSpan.FromSeconds(1f / _melee.GetAttackRate(used, user, melee));
        Dirty(used, melee);

        var t = target.AsNullable();
        foreach (var held in _hands.EnumerateHeld(t))
        {
            if (!_hands.TryDrop(t, held, doDropInteraction: false))
                continue;

            _hands.TryPickupAnyHand(user, held, checkActionBlocker: false);
            break;
        }
    }
}
