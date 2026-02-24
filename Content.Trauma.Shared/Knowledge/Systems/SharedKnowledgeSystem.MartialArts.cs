using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.MartialArts;
using Content.Trauma.Shared.MartialArts.Components;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Systems;
public abstract partial class SharedKnowledgeSystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private static readonly EntProtoId StrengthKnowledge = "StrengthKnowledge";
    private static readonly EntProtoId AthleticsKnowledge = "AthleticsKnowledge";
    private static readonly EntProtoId MeleeKnowledge = "MeleeKnowledge";
    private static readonly EntProtoId ToughnessKnowledge = "ToughnessKnowledge";

    private void InitializeMartialArts()
    {
        SubscribeLocalEvent<KnowledgeHolderComponent, ShotAttemptedEvent>(OnShotAttempt);
        SubscribeLocalEvent<NoGunComponent, ShotAttemptedEvent>(OnShotAttemptKnowledge);
        SubscribeLocalEvent<KnowledgeHolderComponent, BeforeInteractHandEvent>(OnInteract);
        SubscribeLocalEvent<KnowledgeHolderComponent, ComboAttackPerformedEvent>(OnComboAttackPerformed);
        SubscribeLocalEvent<KnowledgeHolderComponent, SaveLastAttacksEvent>(OnSave);
        SubscribeLocalEvent<KnowledgeHolderComponent, ResetLastAttacksEvent>(OnReset);
        SubscribeLocalEvent<KnowledgeHolderComponent, LoadLastAttacksEvent>(OnLoad);
        SubscribeLocalEvent<MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<KnowledgeHolderComponent, BeforeStaminaDamageEvent>(OnStaminaTakeDamage);
        SubscribeLocalEvent<KnowledgeHolderComponent, BeforeDamageChangedEvent>(OnTakeDamage);

        SubscribeNetworkEvent<KnowledgeUpdateMartialArtsEvent>(OnUpdateMartialArts);
    }

    private void OnShotAttempt(Entity<KnowledgeHolderComponent> ent, ref ShotAttemptedEvent args)
    {
        if (ent.Comp.KnowledgeEntity is not { } knowledgeEnt || !TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledge))
            return;

        if (knowledge.MartialArtSkillUid is not { } martialArtUid || !HasComp<MartialArtsKnowledgeComponent>(martialArtUid))
            return;

        RaiseLocalEvent(martialArtUid, ref args);

        if (args.Cancelled)
            _popup.PopupClient(Loc.GetString("gun-disabled"), ent, ent);
    }

    private void OnShotAttemptKnowledge(Entity<NoGunComponent> ent, ref ShotAttemptedEvent args)
    {
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

        var bonus = 0f;
        if (TryGetKnowledgeUnit(ent, StrengthKnowledge) is { } strength)
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

        if (TryGetKnowledgeUnit(ent, MeleeKnowledge) is { } melee)
        {
            args.ModifiersList.Add(new DamageModifierSet()
            {
                FlatReduction = new Dictionary<string, float>()
                {
                    ["Brute"] = -3 * SharpCurve(melee) * Math.Min(GetMastery(melee) - 3, 0) * Math.Min(GetMastery(melee) - 3, 0)
                }
            }); //Provide Armor Piercing at high melee
        }


        if (GetActiveMartialArt(ent) is { } martialArt)
        {
            if (TryComp<SneakAttackComponent>(martialArt, out var sneakAttack))
            {
                sneakAttack.FramesTillHidden = _timing.CurTime + TimeSpan.FromSeconds(sneakAttack.SecondsTillHidden);
                sneakAttack.IsFound = true;
                Dirty(martialArt, sneakAttack);
            }
            if (TryComp<SneakAttackComponent>(martialArt, out var speedArt))
            {
                speedArt.FramesTillHidden = _timing.CurTime + TimeSpan.FromSeconds(speedArt.SecondsTillHidden);
                Dirty(martialArt, speedArt);
            }
        }

        args.BonusDamage += (args.BaseDamage * bonus);
    }

    private void OnStaminaTakeDamage(Entity<KnowledgeHolderComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        if (ent.Comp.KnowledgeEntity is not { } knowledgeEnt || !TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledgeContainerComp))
            return;

        if (TryGetKnowledgeUnit(ent, AthleticsKnowledge) is { } athletics)
        {
            if (args.Value > 0)
                args.Value *= 1 - 0.99f * SharpCurve(athletics);
        }
        if (args.Value > 0 && _mobState.IsAlive(ent))
        {
            var ev = new AddExperienceEvent(AthleticsKnowledge, Math.Min((int) args.Value / 5, 10));
            RaiseLocalEvent(ent, ref ev);
        }
    }

    private void OnTakeDamage(Entity<KnowledgeHolderComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (TryGetKnowledgeUnit(ent, ToughnessKnowledge) is { } toughness && _mobState.IsAlive(ent.Owner))
        {
            if (args.Damage.GetTotal() > 0)
                args.Damage *= 1 - 0.99f * SharpCurve(toughness);
        }
        if (args.Damage.GetTotal() > 0 && _mobState.IsAlive(ent))
        {
            var ev = new AddExperienceEvent(ToughnessKnowledge, Math.Min((int) args.Damage.GetTotal() / 5, 10));
            RaiseLocalEvent(ent, ref ev);
        }
        if (GetActiveMartialArt(ent) is { } martialArt && TryComp<SneakAttackComponent>(martialArt, out var sneakAttack))
        {
            sneakAttack.FramesTillHidden = _timing.CurTime;
            sneakAttack.IsFound = true;
            Dirty(martialArt, sneakAttack);
        }
    }

    private void OnUpdateMartialArts(KnowledgeUpdateMartialArtsEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } player)
            return;

        var knowledgeUid = GetEntity(ev.Knowledge);

        if (TryGetKnowledgeEntity(player) is not { } knowledgeEnt || !TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledgeContainerComp))
            return;

        knowledgeContainerComp.MartialArtSkillUid = knowledgeUid;
        Dirty(knowledgeEnt, knowledgeContainerComp);
    }

    private EntityUid? GetActiveMartialArt(EntityUid target)
    {
        if (TryGetKnowledgeEntity(target) is { } brainActual && TryComp<KnowledgeContainerComponent>(brainActual, out var knowledgeContainerComp) && knowledgeContainerComp.MartialArtSkillUid is { } martialArt)
            return martialArt;
        return null;
    }
}
