using Content.Shared.Actions;
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
using Content.Trauma.Shared.MartialArts.Events;
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

    private float SharpCurve(KnowledgeComponent knowledge)
    {
        return ((float) knowledge.Level / 100.0f) * ((float) knowledge.Level / 100.0f);
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
            bonus += 3 * SharpCurve(strength);
            args.ModifiersList.Add(new DamageModifierSet()
            {
                FlatReduction = new Dictionary<string, float>()
                {
                    ["Brute"] = -5 * SharpCurve(strength) * Math.Min(GetMastery(strength) - 3, 0) * Math.Min(GetMastery(strength) - 3, 0)
                }
            }); //Provide Armor Piercing at high strength
        }

        if (TryComp<KnowledgeComponent>(TryGetKnowledgeUnit(ent, "AthleticsKnowledge"), out var athletics))
        {
            bonus += 0.7f * SharpCurve(athletics);
        }

        if (TryComp<KnowledgeComponent>(TryGetKnowledgeUnit(ent, "ToughnessKnowledge"), out var toughness))
        {
            bonus += 1.5f * SharpCurve(toughness);
            args.ModifiersList.Add(new DamageModifierSet()
            {
                FlatReduction = new Dictionary<string, float>()
                {
                    ["Brute"] = -2 * SharpCurve(toughness) * Math.Min(GetMastery(toughness) - 3, 0) * Math.Min(GetMastery(toughness) - 3, 0)
                }
            }); //Provide Armor Piercing at high toughness
        }

        if (TryComp<KnowledgeComponent>(TryGetKnowledgeUnit(ent, "MeleeKnowledge"), out var melee))
        {
            bonus += 2 * SharpCurve(melee);
            args.ModifiersList.Add(new DamageModifierSet()
            {
                FlatReduction = new Dictionary<string, float>()
                {
                    ["Brute"] = -1 * SharpCurve(melee) * Math.Min(GetMastery(melee) - 3, 0) * Math.Min(GetMastery(melee) - 3, 0)
                }
            }); //Provide Armor Piercing at high melee
        }

        args.BonusDamage += (args.BaseDamage * bonus / 100);

        if (_netManager.IsClient)
            return;

        if (TryComp<KnowledgeComponent>(TryGetKnowledgeUnit(ent, "MeleeKnowledge"), out melee))
        {
            if (GetMastery(melee) < 2)
            {
                FailMelee(melee.Level + melee.TemporaryLevel, ref args);
                args.Handled = true;
                return;
            }
        }
        else
        {
            FailMelee(0, ref args);
            args.Handled = true;
            return;
        }

        var expToSend = 0;
        foreach (var hitEntity in args.HitEntities)
        {
            if (!HasComp<MobStateComponent>(hitEntity))
                continue;

            if (_mobState.IsDead(hitEntity) || _mobState.IsHardCrit(hitEntity))
                continue;

            if (hitEntity == ent)
                continue;
            expToSend++;
        }
        if (expToSend > 0)
        {
            var ev = new AddExperience("MeleeKnowledge", expToSend);
            RaiseLocalEvent(ent, ref ev);
        }
        var evStr = new AddExperience("StrengthKnowledge", 1);
        RaiseLocalEvent(ent, ref evStr);
    }

    private void FailMelee(int level, ref MeleeHitEvent args)
    {
        float failChance = Math.Clamp(1f - ((float) (level) / 26f), 0, 1f);

        if (_random.Prob(failChance))
        {
            _damageable.TryChangeDamage(args.User, args.BaseDamage + args.BonusDamage);

            // 3. Visual/Audio Feedback
            _popup.PopupEntity(Loc.GetString("melee-clumsy-self-hit"), args.User, args.User, PopupType.LargeCaution);
            _audio.PlayPvs(_clumsySound, args.User);

            Log.Debug($"Melee attack failed due to low skill level. Fail Chance: {failChance * 100}%");
            return;
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
        }
        var ev = new AddExperience("AthleticsKnowledge", 1);
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnTakeDamage(Entity<KnowledgeHolderComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (TryComp<KnowledgeComponent>(TryGetKnowledgeUnit(ent, "ToughnessKnowledge"), out var toughness) && _mobState.IsAlive(ent.Owner))
        {
            if (args.Damage.GetTotal() > 0)
                args.Damage *= 1 - 0.99f * SharpCurve(toughness);
        }
        if (args.Damage.GetTotal() > 0 && _mobState.IsAlive(ent))
        {
            var ev = new AddExperience("ToughnessKnowledge", Math.Min((int) args.Damage.GetTotal() / 5, 20));
            RaiseLocalEvent(ent, ref ev);
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
