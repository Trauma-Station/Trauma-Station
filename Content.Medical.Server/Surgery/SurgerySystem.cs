// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Targeting;
using Content.Medical.Shared.Surgery;
using Content.Medical.Shared.Surgery.Effects.Step;
using Content.Medical.Shared.Wounds;
using Content.Server.Chat.Systems;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;

namespace Content.Medical.Server.Surgery;

public sealed class SurgerySystem : SharedSurgerySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly WoundSystem _wounds = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryTargetComponent, SurgeryStepDamageEvent>(OnSurgeryStepDamage);
        // You might be wondering "why aren't we using StepEvent for these two?" reason being that StepEvent fires off regardless of success on the previous functions
        // so this would heal entities even if you had a used or incorrect organ.
        SubscribeLocalEvent<SurgeryDamageChangeEffectComponent, SurgeryStepDamageChangeEvent>(OnSurgeryDamageChange);
        SubscribeLocalEvent<SurgeryStepEmoteEffectComponent, SurgeryStepEvent>(OnStepScreamComplete);
        SubscribeLocalEvent<SurgeryStepSpawnEffectComponent, SurgeryStepEvent>(OnStepSpawnComplete);
    }

    private void SetDamage(EntityUid body,
        DamageSpecifier damage,
        float partMultiplier,
        EntityUid user,
        EntityUid part,
        bool affectAll = false)
    {
        // kinda funky but still works
        // TODO: Also the scar treating surgery too, fuck. I hate this system and by every second I have to spend working with THIS I want to kill myself more and more
        _wounds.TryHaltAllBleeding(part, force: true);
        _damageable.TryChangeDamage(body,
            damage,
            true,
            origin: user,
            partMultiplier: partMultiplier,
            targetPart: affectAll ? TargetBodyPart.All : _part.GetTargetBodyPart(part),
            ignoreBlockers: true);
    }

    private void OnSurgeryStepDamage(Entity<SurgeryTargetComponent> ent, ref SurgeryStepDamageEvent args) =>
        SetDamage(args.Body, args.Damage, args.PartMultiplier, args.User, args.Part);

    private void OnSurgeryDamageChange(Entity<SurgeryDamageChangeEffectComponent> ent, ref SurgeryStepDamageChangeEvent args)
    {
        var damageChange = ent.Comp.Damage;
        if (Status.HasEffectComp<ForcedSleepingStatusEffectComponent>(args.Body))
            damageChange = damageChange * ent.Comp.SleepModifier;

        SetDamage(args.Body, damageChange, 0.5f, args.User, args.Part, ent.Comp.AffectAll);
    }
    private void OnStepScreamComplete(Entity<SurgeryStepEmoteEffectComponent> ent, ref SurgeryStepEvent args)
    {
        if (Status.HasEffectComp<ForcedSleepingStatusEffectComponent>(args.Body))
            return;

        _chat.TryEmoteWithChat(args.Body, ent.Comp.Emote, voluntary: false);
    }
    private void OnStepSpawnComplete(Entity<SurgeryStepSpawnEffectComponent> ent, ref SurgeryStepEvent args) =>
        SpawnAtPosition(ent.Comp.Entity, Transform(args.Body).Coordinates);
}
