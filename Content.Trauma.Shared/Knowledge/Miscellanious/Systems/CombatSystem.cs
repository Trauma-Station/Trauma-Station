// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Events;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Attribute.Attribute.Components;
using Content.Trauma.Shared.Knowledge.FightingStance;
using Content.Trauma.Shared.Knowledge.Miscellanious.Components;
using Content.Trauma.Shared.Knowledge.Systems;
using Content.Trauma.Shared.Parry;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Knowledge.Miscellanious.Systems;

public sealed partial class CombatSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedKnowledgeSystem _knowledge = default!;

    private SoundSpecifier _parrySound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/parry.ogg", AudioParams.Default.WithVariation(0.05f));
    private EntProtoId _dodgeTalent = "DodgeTalent";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, ActiveMeleeResolveEvent>(ResolveAttack);
        SubscribeLocalEvent<KnowledgeHolderComponent, ProjectileReflectAttemptEvent>(TryDodgeProjectile);
        SubscribeLocalEvent<KnowledgeHolderComponent, HitScanReflectAttemptEvent>(TryDodgeHitscan);
        SubscribeLocalEvent<KnowledgeHolderComponent, GetDefenseDice>(CalculateDefenseDice);
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

        if (evOpposedContest.CriticallyFailedUser && evOpposedContest.CriticallyFailedOpposed)
        {
            args.Cancelled = true;
            _popup.PopupClient("You try to strike the enemy, but end up not doing much of anything.", ent, ent, PopupType.Small);
            _popup.PopupEntity("You stumble around like a bummbling fool, not doing anything effect.", defender, defender, PopupType.Small);
        }

        if (evOpposedContest.Failed)
        {
            if (evOpposedContest.CriticallySucceededUser)
                return; // If you crit but can't strike the opponent, then what are you fighting?

            if (evOpposedContest.CriticallyFailedUser)
            {
                var fumbleEv = new OnFumbleEvent(evOpposedContest.DiceOpposed + evOpposedContest.ModOpposed - evOpposedContest.DiceUser - evOpposedContest.ModUser);
                RaiseLocalEvent(attacker, ref fumbleEv);
            }

            var parrySound = _parrySound;
            if (TryComp<ParryComponent>(args.Weapon, out var parryComp))
                parrySound = parryComp.SoundOnParry;
            _audio.PlayLocal(parrySound, defender, _player.LocalEntity);
            if (evOpposedContest.ModOpposed >= 19)
            {
                var queued = AddComp<QueuedStrikeComponent>(defender); // Defender gets a free strike.
                queued.TimeToHit = _timing.CurTime + TimeSpan.FromSeconds(1); // Hit next second.
                queued.Target = ent;
                queued.Offhand = !evOpposedContest.CriticallySucceededOpposed;
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

        if (evOpposedContest.CriticallyFailedOpposed)
        {
            _popup.PopupClient("You missed, but it could have been worse.", ent, ent, PopupType.Small);
            args.Cancelled = true;
            return;
        }

        if (evOpposedContest.CriticallySucceededUser)
        {
            var ev = new CriticalHitEvent(attacker, args.Damage);
            RaiseLocalEvent(defender, ref ev);
            _popup.PopupClient("Good strike!", ent, ent, PopupType.Small);
        }
    }

    private void TryDodgeProjectile(Entity<KnowledgeHolderComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        if (TryDodge(ent, args.ProjUid))
            args.Cancelled = true;
    }

    private void TryDodgeHitscan(Entity<KnowledgeHolderComponent> ent, ref HitScanReflectAttemptEvent args)
    {
        if (TryDodge(ent, args.SourceItem))
            args.Reflected = true;
    }

    private bool TryDodge(Entity<KnowledgeHolderComponent> ent, EntityUid projectile)
    {
        if (_mobState.IsIncapacitated(ent.Owner) || !HasComp<MobStateComponent>(ent.Owner)) // ever seen a corpse parry? Can't say I have.
            return false;

        int defense = 0;
        if (_knowledge.GetContainer(ent.Owner) is { } brain && _knowledge.GetTalent(brain, _dodgeTalent) is { } talent)
        {
            var defenseEv = new GetDefenseModifierEvent();
            RaiseLocalEvent(ent, ref defenseEv);
            defense += defenseEv.Mod;
        }

        // TODO: Replace with gun attack thing.
        var ev = new SingleContestEvent(20, defense, 20);
        RaiseLocalEvent(ent, ref ev);
        return !ev.Failed;
    }

    private void CalculateDefenseDice(Entity<KnowledgeHolderComponent> ent, ref GetDefenseDice args)
    {
        if (!_mobState.IsAlive(ent))
            return;

        if (!TryComp<FightingStanceComponent>(ent, out var fighting))
        {
            args.Dice = 12;
            return;
        }

        args.Dice = fighting.DefenseDice;
    }
}
