// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Blocking;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Melee;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.FightingStance;
using Content.Trauma.Shared.Knowledge.Quality;
using Content.Trauma.Shared.Knowledge.Systems;

namespace Content.Trauma.Shared.Knowledge.Miscellanious.Systems;
public sealed partial class FightingStanceSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedKnowledgeSystem _knowledge = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, EquippedHandEvent>(OnHandsChanged);
        SubscribeLocalEvent<KnowledgeHolderComponent, UnequippedHandEvent>(OnHandsChanged);
        SubscribeLocalEvent<KnowledgeHolderComponent, HandSelectedEvent>(OnHandsChanged);
        SubscribeLocalEvent<KnowledgeHolderComponent, WieldAttemptEvent>(OnHandsChanged);
    }

    private void OnHandsChanged(Entity<KnowledgeHolderComponent> ent, ref EquippedHandEvent args) => FigureOutFightingStyle(ent);
    private void OnHandsChanged(Entity<KnowledgeHolderComponent> ent, ref UnequippedHandEvent args) => FigureOutFightingStyle(ent);
    private void OnHandsChanged(Entity<KnowledgeHolderComponent> ent, ref HandSelectedEvent args) => FigureOutFightingStyle(ent);
    private void OnHandsChanged(Entity<KnowledgeHolderComponent> ent, ref WieldAttemptEvent args) => FigureOutFightingStyle(ent);


    private void FigureOutFightingStyle(Entity<KnowledgeHolderComponent> ent)
    {
        var weaponCount = 0;
        var wieldCount = 0;
        var shieldCount = 0;
        var qualityAdjustment = 0;

        if (_knowledge.GetContainer(ent) is not { } brain)
            return;

        EnsureComp<FightingStanceComponent>(ent, out var fighting);
        fighting.AttackMod = 0;
        fighting.DamageMod = 0;
        fighting.DefenseMod = 0;
        fighting.SpeedMod = 0;
        fighting.DefenseDice = 12; // Without a fighting stance, your defense is shit.

        foreach (var hand in _hands.EnumerateHands(ent.Owner))
        {
            if (!_hands.TryGetHeldItem(ent.Owner, hand, out var item))
                continue;

            // Check proficiency. Can't use a fighting style if you don't have the proficiency.


            /*

            if (Prototype(item.Value) is { } proto && brain.Comp.WeaponSpecializations.TryGetValue(proto, out var spec))
            {
                fighting.AttackMod += spec.Attack;
                fighting.DamageMod += spec.Damage;
                fighting.DefenseMod += spec.Defense;
                fighting.SpeedMod += spec.Speed;
            }

            */

            if (HasComp<MeleeWeaponComponent>(item))
                weaponCount++;

            if (HasComp<BlockingComponent>(item))
                shieldCount++;

            if (HasComp<WieldableComponent>(item))
                shieldCount++;

            if (TryComp<QualityComponent>(item, out var comp))
                qualityAdjustment += comp.Quality;
        }

        foreach (var stance in _proto.EnumeratePrototypes<FightingStancePrototype>())
        {
            if (stance.WeaponCount >= weaponCount && stance.WieldCount >= wieldCount && stance.ShieldCount >= shieldCount)
            {
                fighting.AttackMod = stance.AttackMod + qualityAdjustment;
                fighting.DefenseMod = stance.DefenseMod + qualityAdjustment;
                fighting.SpeedMod = stance.SpeedMod + qualityAdjustment;
                fighting.DamageMod = stance.DamageMod + qualityAdjustment;
                fighting.DefenseDice = stance.DefenseDice + qualityAdjustment;
                break;
            }
        }
    }
}
