// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Blocking;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Attribute.Attribute.Components;
using Content.Trauma.Shared.Knowledge.FightingStance;
using Content.Trauma.Shared.Knowledge.Miscellanious.Components;
using Content.Trauma.Shared.Knowledge.Quality;
using Content.Trauma.Shared.Knowledge.Systems;
using Content.Trauma.Shared.Parry;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Knowledge.Miscellanious.Systems;

public sealed partial class MiscAttributeSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedKnowledgeSystem _knowledge = default!;

    private SoundSpecifier _parrySound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/parry.ogg", AudioParams.Default.WithVariation(0.05f));

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, ActiveMeleeResolveEvent>(ResolveAttack);
        SubscribeLocalEvent<KnowledgeHolderComponent, GetDefenseDice>(CalculateDefenseDice);
        SubscribeLocalEvent<KnowledgeHolderComponent, EquippedHandEvent>(OnHandsChanged);
        SubscribeLocalEvent<KnowledgeHolderComponent, UnequippedHandEvent>(OnHandsChanged);
        SubscribeLocalEvent<KnowledgeHolderComponent, HandSelectedEvent>(OnHandsChanged);
        SubscribeLocalEvent<KnowledgeHolderComponent, WieldAttemptEvent>(OnHandsChanged);
    }

    private void ResolveAttack(Entity<KnowledgeHolderComponent> ent, ref ActiveMeleeResolveEvent args)
    {
        var attacker = ent.Owner;
        var defender = args.Defender;

        if (_mobState.IsIncapacitated(defender) || !HasComp<MobStateComponent>(defender) || attacker == defender) // ever seen a corpse parry? Can't say I have.
            return;

        var evAttackMod = new GetAttackModifierEvent();
        RaiseLocalEvent(attacker, ref evAttackMod);

        var evDefenseMod = new GetDefenseModifierEvent();
        RaiseLocalEvent(defender, ref evDefenseMod);

        var evDefenseDice = new GetDefenseDice(8);
        RaiseLocalEvent(defender, ref evDefenseDice);

        var evOpposedContest = new OpposedContestEvent(defender, 20, evAttackMod.Mod, evDefenseDice.Dice, evDefenseMod.Mod);
        RaiseLocalEvent(attacker, ref evOpposedContest);

        // Makes it harder to defend after being hit, the beatdown is gonna be brutal if you're surrounded.
        if (!TryComp<DefenseTierdownComponent>(defender, out var defenseDown))
            defenseDown = AddComp<DefenseTierdownComponent>(defender);
        defenseDown.Mod += 1.0f;
        Dirty(defender, defenseDown);

        if (evOpposedContest.Failed)
        {
            var parrySound = _parrySound;
            if (TryComp<ParryComponent>(args.Weapon, out var parryComp))
                parrySound = parryComp.SoundOnParry;
            _audio.PlayLocal(parrySound, defender, _player.LocalEntity);
            if (evOpposedContest.CriticallySucceededOpposed)
            {
                var queued = AddComp<QueuedParryComponent>(defender); // Defender gets a free strike.
                queued.TimeToHit = _timing.CurTime + TimeSpan.FromSeconds(1); // Hit next second.
                queued.Target = ent;
                Dirty(defender, queued);
                // TODO: Replace with sound effects to not flood up chat.
                _popup.PopupClient("You've shown an opening!", ent, ent, PopupType.Small);
                _popup.PopupEntity("The opponent has shown an opening, prepare for an attack!", defender, defender, PopupType.Small);
            }
            else
            {
                _popup.PopupClient("You've been parried.", ent, ent, PopupType.Small);
                _popup.PopupEntity("You've successfully defended against an opponent.", defender, defender, PopupType.Small);
            }
            args.Cancelled = true;
            return;
        }
        if (evOpposedContest.CriticallySucceededUser)
        {
            args.Damage *= 2; // Replace with critical hit function?
            _popup.PopupClient("Good strike!", ent, ent, PopupType.Small);
        }
    }

    private void CalculateDefenseDice(Entity<KnowledgeHolderComponent> ent, ref GetDefenseDice args)
    {
        if (!_mobState.IsAlive(ent))
            return;
        args.Dice = 12;
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

            // Check proficiency. Can't use weapon if you don't have the proficiency.

            if (Prototype(item.Value) is { } proto && brain.Comp.WeaponSpecializations.TryGetValue(proto, out var spec))
            {
                fighting.AttackMod += spec.Attack;
                fighting.DamageMod += spec.Damage;
                fighting.DefenseMod += spec.Defense;
                fighting.SpeedMod += spec.Speed;
            }

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
