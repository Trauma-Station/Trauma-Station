// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Body;
using Content.Medical.Shared.Surgery;
using Content.Medical.Shared.Surgery.Conditions;
using Content.Medical.Shared.Surgery.Effects.Step;
using Content.Medical.Shared.Surgery.Tools;
using Content.Server.Atmos.Rotting;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Prototypes;

namespace Content.Medical.Server.Surgery;

public sealed partial class SurgerySystem : SharedSurgerySystem
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    // You might be wondering "why aren't we using StepEvent for these two?" reason being that StepEvent fires off regardless of success on the previous functions
    // so this would heal entities even if you had a used or incorrect organ.
    [SubscribeLocalEvent]
    private void OnSurgeryStepDamage(Entity<SurgeryTargetComponent> ent, ref SurgeryStepDamageEvent args)
    {
        _damageable.ChangeDamage(args.Part, args.Damage, true, origin: args.User, ignoreBlockers: true);
    }

    [SubscribeLocalEvent]
    private void OnSurgeryDamageChange(Entity<SurgeryDamageChangeEffectComponent> ent, ref SurgeryStepDamageChangeEvent args)
    {
        var damageChange = ent.Comp.Damage;
        if (Status.HasEffectComp<ForcedSleepingStatusEffectComponent>(args.Body))
            damageChange *= ent.Comp.SleepModifier;

        _damageable.ChangeDamage(args.Part, damageChange, true, origin: args.User, ignoreBlockers: true);
    }

    [SubscribeLocalEvent]
    private void OnStepScreamComplete(Entity<SurgeryStepEmoteEffectComponent> ent, ref SurgeryStepEvent args)
    {
        if (Status.HasEffectComp<ForcedSleepingStatusEffectComponent>(args.Body))
            return;

        _chat.TryEmoteWithChat(args.Body, ent.Comp.Emote, voluntary: false);
    }

    [SubscribeLocalEvent]
    private void OnStepSpawnComplete(Entity<SurgeryStepSpawnEffectComponent> ent, ref SurgeryStepEvent args)
    {
        SpawnAtPosition(ent.Comp.Entity, Transform(args.Body).Coordinates);
    }
}
