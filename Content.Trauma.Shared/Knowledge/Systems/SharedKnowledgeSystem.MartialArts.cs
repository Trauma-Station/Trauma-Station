using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge.Systems;
using Content.Trauma.Common.MartialArts;
using Content.Trauma.Shared.MartialArts.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.Knowledge.Systems;
public abstract partial class SharedKnowledgeSystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private readonly SoundSpecifier _clumsySound = new SoundPathSpecifier("/Audio/Weapons/rubberhammer.ogg");
    private void InitializeMartialArts()
    {
        SubscribeLocalEvent<KnowledgeHolderComponent, ShotAttemptedEvent>(OnShotAttempt);
        SubscribeLocalEvent<KnowledgeHolderComponent, BeforeInteractHandEvent>(OnInteract);
        SubscribeLocalEvent<KnowledgeHolderComponent, ComboAttackPerformedEvent>(OnComboAttackPerformed);
        SubscribeLocalEvent<KnowledgeHolderComponent, SaveLastAttacksEvent>(OnSave);
        SubscribeLocalEvent<KnowledgeHolderComponent, ResetLastAttacksEvent>(OnReset);
        SubscribeLocalEvent<KnowledgeHolderComponent, LoadLastAttacksEvent>(OnLoad);
        SubscribeLocalEvent<MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<KnowledgeHolderComponent, BeforeStaminaDamageEvent>(OnStaminaTakeDamage);
        SubscribeLocalEvent<KnowledgeHolderComponent, BeforeDamageChangedEvent>(OnTakeDamage);

        SubscribeNetworkEvent<KnowledgeUpdateMartialArts>(OnUpdateMartialArts);
    }

    private void OnShotAttempt(Entity<KnowledgeHolderComponent> ent, ref ShotAttemptedEvent args)
    {
        if (ent.Comp.KnowledgeEntity is not { } knowledgeEnt || !TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledge))
            return;

        if (knowledge.MartialArtSkillUid is not { } martialArtUid || !TryComp<MartialArtsKnowledgeComponent>(martialArtUid, out _) || !TryComp<NoGunComponent>(martialArtUid, out _))
            return;

        _popup.PopupClient(Loc.GetString("gun-disabled"), ent, ent);
        args.Cancel();
    }

    private void OnInteract(Entity<KnowledgeHolderComponent> ent, ref BeforeInteractHandEvent args)
    {
        if (ent.Owner == args.Target || !HasComp<MobStateComponent>(args.Target))
            return;

        if (ent.Comp.KnowledgeEntity is not { } knowledgeEnt || !TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledgeContainerComp))
            return;

        if (knowledgeContainerComp.MartialArtSkillUid is not { } martialArtSkillUid)
            return;

        RaiseLocalEvent(ent.Owner, new ComboAttackPerformedEvent(ent.Owner, args.Target, ent.Owner, ComboAttackType.Hug));
    }

    public void OnComboAttackPerformed(Entity<KnowledgeHolderComponent> ent, ref ComboAttackPerformedEvent args)
    {
        if (ent.Comp.KnowledgeEntity is not { } knowledgeEnt || !TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledgeContainerComp))
            return;

        if (knowledgeContainerComp.MartialArtSkillUid is not { } martialArtSkillUid || !TryComp<MartialArtsKnowledgeComponent>(martialArtSkillUid, out var martialArtComp))
            return;

        RaiseLocalEvent(martialArtSkillUid, args);
    }

    private void OnSave(Entity<KnowledgeHolderComponent> ent, ref SaveLastAttacksEvent args)
    {
        if (ent.Comp.KnowledgeEntity is not { } knowledgeEnt || !TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledgeContainerComp))
            return;

        if (knowledgeContainerComp.MartialArtSkillUid is not { } martialArtSkillUid || !TryComp<MartialArtsKnowledgeComponent>(martialArtSkillUid, out var martialArtComp))
            return;

        RaiseLocalEvent(martialArtSkillUid, args);
    }
    private void OnReset(Entity<KnowledgeHolderComponent> ent, ref ResetLastAttacksEvent args)
    {
        if (ent.Comp.KnowledgeEntity is not { } knowledgeEnt || !TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledgeContainerComp))
            return;

        if (knowledgeContainerComp.MartialArtSkillUid is not { } martialArtSkillUid || !TryComp<MartialArtsKnowledgeComponent>(martialArtSkillUid, out var martialArtComp))
            return;

        RaiseLocalEvent(martialArtSkillUid, args);
    }

    private void OnLoad(Entity<KnowledgeHolderComponent> ent, ref LoadLastAttacksEvent args)
    {
        if (ent.Comp.KnowledgeEntity is not { } knowledgeEnt || !TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledgeContainerComp))
            return;

        if (knowledgeContainerComp.MartialArtSkillUid is not { } martialArtSkillUid || !TryComp<MartialArtsKnowledgeComponent>(martialArtSkillUid, out var martialArtComp))
            return;

        RaiseLocalEvent(martialArtSkillUid, args);
    }

    private void OnMeleeHit(MeleeHitEvent args)
    {
        if (args.Handled)
            return;

        var ent = args.User;

        if (!TryComp<KnowledgeHolderComponent>(ent, out var knowledgeComp) || knowledgeComp.KnowledgeEntity == null)
            return;

        var bonus = 1f;
        if (TryComp<KnowledgeComponent>(TryGetKnowledgeUnit(ent, "StrengthKnowledge"), out var strength))
        {
            var ev = new AddExperience("StrengthKnowledge", 1);
            RaiseLocalEvent(ent, ref ev);
            bonus += 3 * ((float) strength.Level / 100.0f) * ((float) strength.Level / 100.0f);
            args.ModifiersList.Add(new DamageModifierSet()
            {
                FlatReduction = new Dictionary<string, float>()
                {
                    ["Brute"] = -5 * ((float) strength.Level / 100.0f) * ((float) strength.Level / 100.0f) * Math.Min(GetMastery(strength) - 3, 0) * Math.Min(GetMastery(strength) - 3, 0)
                }
            }); //Provide Armor Piercing at high strength
        }
        if (TryComp<KnowledgeComponent>(TryGetKnowledgeUnit(ent, "AthleticsKnowledge"), out var athletics))
        {
            bonus += 0.7f * ((float) athletics.Level / 100.0f) * ((float) athletics.Level / 100.0f);
        }
        if (TryComp<KnowledgeComponent>(TryGetKnowledgeUnit(ent, "ToughnessKnowledge"), out var toughness))
        {
            bonus += 1.5f * ((float) toughness.Level / 100.0f) * ((float) toughness.Level / 100.0f);
            args.ModifiersList.Add(new DamageModifierSet()
            {
                FlatReduction = new Dictionary<string, float>()
                {
                    ["Brute"] = -2 * ((float) toughness.Level / 100.0f) * ((float) toughness.Level / 100.0f) * Math.Min(GetMastery(toughness) - 3, 0) * Math.Min(GetMastery(toughness) - 3, 0)
                }
            }); //Provide Armor Piercing at high toughness
        }
        if (TryComp<KnowledgeComponent>(TryGetKnowledgeUnit(ent, "MeleeKnowledge"), out var melee))
        {
            var expToSend = 0;
            foreach (var hitEntity in args.HitEntities)
            {
                if (!HasComp<MobStateComponent>(hitEntity))
                    continue;

                if (_mobState.IsDead(hitEntity))
                    continue;

                if (hitEntity == ent)
                    continue;
                expToSend += 1;
            }
            if (expToSend > 0)
            {
                var ev = new AddExperience("MeleeKnowledge", expToSend);
                RaiseLocalEvent(ent, ref ev);
            }
            bonus += 2 * ((float) melee.Level / 100.0f) * ((float) melee.Level / 100.0f);
            args.ModifiersList.Add(new DamageModifierSet()
            {
                FlatReduction = new Dictionary<string, float>()
                {
                    ["Brute"] = -1 * ((float) melee.Level / 100.0f) * ((float) melee.Level / 100.0f) * Math.Min(GetMastery(melee) - 3, 0) * Math.Min(GetMastery(melee) - 3, 0)
                }
            }); //Provide Armor Piercing at high melee
        }

        args.BonusDamage += (args.BaseDamage * bonus / 100);

        if (TryComp<KnowledgeComponent>(TryGetKnowledgeUnit(ent, "MeleeKnowledge"), out melee))
        {
            if (GetMastery(melee) < 2)
            {
                float failChance = Math.Clamp(1f - ((float) melee.Level / 26f), 0, 1f);

                if (_random.Prob(failChance))
                {
                    _damageable.TryChangeDamage(ent, args.BaseDamage + args.BonusDamage);

                    // 3. Visual/Audio Feedback
                    _popup.PopupEntity(Loc.GetString("melee-clumsy-self-hit"), ent, ent, PopupType.LargeCaution);
                    _audio.PlayPvs(_clumsySound, ent);

                    args.Handled = true;
                    return;
                }
            }
        }
    }

    private void OnStaminaTakeDamage(Entity<KnowledgeHolderComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        if (ent.Comp.KnowledgeEntity is not { } knowledgeEnt || !TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledgeContainerComp))
            return;

        if (TryComp<KnowledgeComponent>(TryGetKnowledgeUnit(ent, "AthleticsKnowledge"), out var athletics))
        {
            if (args.Value > 0)
                args.Value *= 1 - 1.1f * ((float) athletics.Level / 100.0f) * ((float) athletics.Level / 100.0f);
            else
                args.Value *= 10 * ((float) athletics.Level / 100.0f) * ((float) athletics.Level / 100.0f);
            var ev = new AddExperience("AthleticsKnowledge", 1);
            RaiseLocalEvent(ent, ref ev);
        }
    }

    private void OnTakeDamage(Entity<KnowledgeHolderComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (TryComp<KnowledgeComponent>(TryGetKnowledgeUnit(ent, "ToughnessKnowledge"), out var toughness))
        {
            if (args.Damage.GetTotal() > 0)
            {
                args.Damage *= 1 - 1.1f * ((float) toughness.Level / 100.0f) * ((float) toughness.Level / 100.0f);
                var ev = new AddExperience("ToughnessKnowledge", Math.Max((int) args.Damage.GetTotal() / 10, 10));
                RaiseLocalEvent(ent, ref ev);
            }
            else
                args.Damage *= 10 * ((float) toughness.Level / 100.0f) * ((float) toughness.Level / 100.0f);
        }
    }

    private void OnUpdateMartialArts(KnowledgeUpdateMartialArts ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } player)
            return;

        var knowledgeUid = EntityManager.GetEntity(ev.Knowledge);

        if (TryGetKnowledgeEntity(player) is not { } knowledgeEnt)
            return;
        if (!TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledgeContainerComp))
            return;

        knowledgeContainerComp.MartialArtSkillUid = knowledgeUid;
        Dirty(knowledgeEnt, knowledgeContainerComp);
    }
}
