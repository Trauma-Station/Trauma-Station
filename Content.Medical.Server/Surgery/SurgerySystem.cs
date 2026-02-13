// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Targeting;
using Content.Medical.Common.Damage;
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
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Prototypes;
using Content.Medical.Shared.Wounds;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.Weapons.Melee.Events;
using System.Linq;

namespace Content.Medical.Server.Surgery;

public sealed class SurgerySystem : SharedSurgerySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly WoundSystem _wounds = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

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

    protected override void RefreshUI(EntityUid body)
    {
        var surgeries = new Dictionary<NetEntity, List<EntProtoId>>();
        foreach (var part in _body.GetExternalOrgans(body))
        {
            var valid = new List<EntProtoId>();
            foreach (var surgery in AllSurgeries)
            {
                if (GetSingleton(surgery) is not { } surgeryEnt)
                    continue;

                var ev = new SurgeryValidEvent(body, part);
                RaiseLocalEvent(surgeryEnt, ref ev);

                if (ev.Cancelled)
                    continue;

                valid.Add(surgery);
            }
            surgeries[GetNetEntity(part)] = valid;
        }
        _ui.SetUiState(body, SurgeryUIKey.Key, new SurgeryBuiState(surgeries));
        /*
            TODO SHITMED: fuck you mocho, investigate why this actually happens
            Reason we do this is because when applying a BUI State, it rolls back the state on the entity temporarily,
            which just so happens to occur right as we're checking for step completion, so we end up with the UI
            not updating at all until you change tools or reopen the window. I love shitcode.
        */
        _ui.ServerSendUiMessage(body, SurgeryUIKey.Key, new SurgeryBuiRefreshMessage());
    }

    private DamageGroupPrototype? GetDamageGroupByType(string id)
    {
        return (from @group in _proto.EnumeratePrototypes<DamageGroupPrototype>() where @group.DamageTypes.Contains(id) select @group).FirstOrDefault();
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
            targetPart: affectAll ? TargetBodyPart.All : _part.GetTargetBodyPart(part));
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
