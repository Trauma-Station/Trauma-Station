using System.Linq;
using Content.Goobstation.Shared.Enchanting.Components;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge.Systems;
using Content.Trauma.Common.MartialArts;
using Content.Trauma.Shared.MartialArts.Components;
using Robust.Shared.Toolshed.Syntax;

namespace Content.Trauma.Shared.Knowledge.Systems;
public abstract partial class SharedKnowledgeSystem
{
    private void InitializeMartialArts()
    {
        SubscribeLocalEvent<KnowledgeHolderComponent, ShotAttemptedEvent>(OnShotAttempt);
        SubscribeLocalEvent<KnowledgeHolderComponent, BeforeInteractHandEvent>(OnInteract);
        SubscribeLocalEvent<KnowledgeHolderComponent, ComboAttackPerformedEvent>(OnComboAttackPerformed);
        SubscribeLocalEvent<KnowledgeHolderComponent, SaveLastAttacksEvent>(OnSave);
        SubscribeLocalEvent<KnowledgeHolderComponent, ResetLastAttacksEvent>(OnReset);
        SubscribeLocalEvent<KnowledgeHolderComponent, LoadLastAttacksEvent>(OnLoad);
        SubscribeLocalEvent<MeleeHitEvent>(OnMeleeHit);

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

        RaiseLocalEvent(martialArtSkillUid, ref args);
    }

    private void OnSave(Entity<KnowledgeHolderComponent> ent, ref SaveLastAttacksEvent args)
    {
        if (ent.Comp.KnowledgeEntity is not { } knowledgeEnt || !TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledgeContainerComp))
            return;

        if (knowledgeContainerComp.MartialArtSkillUid is not { } martialArtSkillUid || !TryComp<MartialArtsKnowledgeComponent>(martialArtSkillUid, out var martialArtComp))
            return;

        RaiseLocalEvent(martialArtSkillUid, ref args);
    }
    private void OnReset(Entity<KnowledgeHolderComponent> ent, ref ResetLastAttacksEvent args)
    {
        if (ent.Comp.KnowledgeEntity is not { } knowledgeEnt || !TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledgeContainerComp))
            return;

        if (knowledgeContainerComp.MartialArtSkillUid is not { } martialArtSkillUid || !TryComp<MartialArtsKnowledgeComponent>(martialArtSkillUid, out var martialArtComp))
            return;

        RaiseLocalEvent(martialArtSkillUid, ref args);
    }

    private void OnLoad(Entity<KnowledgeHolderComponent> ent, ref LoadLastAttacksEvent args)
    {
        if (ent.Comp.KnowledgeEntity is not { } knowledgeEnt || !TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledgeContainerComp))
            return;

        if (knowledgeContainerComp.MartialArtSkillUid is not { } martialArtSkillUid || !TryComp<MartialArtsKnowledgeComponent>(martialArtSkillUid, out var martialArtComp))
            return;

        RaiseLocalEvent(martialArtSkillUid, ref args);
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
            RaiseLocalEvent(ent, ev);
            bonus += 3 * ((float) strength.Level / 100.0f) * ((float) strength.Level / 100.0f);
            args.ModifiersList.Add(new DamageModifierSet()
            {
                FlatReduction = new Dictionary<string, float>()
                {
                    ["Brute"] = -5 * ((float) strength.Level / 100.0f) * ((float) strength.Level / 100.0f) * Math.Clamp(GetMastery(strength) - 3, 0, GetMastery(strength)) * Math.Clamp(GetMastery(strength) - 3, 0, GetMastery(strength))
                }
            }); //Provide Armor Piercing at high strength
            Log.Debug((-5 * ((float) strength.Level / 100.0f) * ((float) strength.Level / 100.0f) * Math.Clamp(GetMastery(strength) - 3, 0, GetMastery(strength)) * Math.Clamp(GetMastery(strength) - 3, 0, GetMastery(strength))).ToString());
        }
        if (TryComp<KnowledgeComponent>(TryGetKnowledgeUnit(ent, "AthleticsKnowledge"), out var athletics))
        {
            var ev = new AddExperience("AthleticsKnowledge", 1);
            RaiseLocalEvent(ent, ev);
            bonus += 0.7f * ((float) athletics.Level / 100.0f) * ((float) athletics.Level / 100.0f);
        }
        if (TryComp<KnowledgeComponent>(TryGetKnowledgeUnit(ent, "ToughnessKnowledge"), out var toughness))
        {
            var ev = new AddExperience("ToughnessKnowledge", 1);
            RaiseLocalEvent(ent, ev);
            bonus += 1.5f * ((float) toughness.Level / 100.0f) * ((float) toughness.Level / 100.0f);
            args.ModifiersList.Add(new DamageModifierSet()
            {
                FlatReduction = new Dictionary<string, float>()
                {
                    ["Brute"] = -2 * ((float) toughness.Level / 100.0f) * ((float) toughness.Level / 100.0f) * Math.Clamp(GetMastery(toughness) - 3, 0, GetMastery(toughness)) * Math.Clamp(GetMastery(toughness) - 3, 0, GetMastery(toughness))
                }
            }); //Provide Armor Piercing at high toughness
        }
        if (TryComp<KnowledgeComponent>(TryGetKnowledgeUnit(ent, "MeleeKnowledge"), out var melee))
        {
            var ev = new AddExperience("MeleeKnowledge", 1);
            RaiseLocalEvent(ent, ev);
            bonus += 2 * ((float) melee.Level / 100.0f) * ((float) melee.Level / 100.0f);
            args.ModifiersList.Add(new DamageModifierSet()
            {
                FlatReduction = new Dictionary<string, float>()
                {
                    ["Brute"] = -1 * ((float) melee.Level / 100.0f) * ((float) melee.Level / 100.0f) * Math.Clamp(GetMastery(melee) - 3, 0, GetMastery(melee)) * Math.Clamp(GetMastery(melee) - 3, 0, GetMastery(melee))
                }
            }); //Provide Armor Piercing at high melee
        }
        args.BonusDamage += (args.BaseDamage * bonus / 100);
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
