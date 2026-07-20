// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Religion;
using Content.Goobstation.Shared.Bible;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Timing;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Goobstation.Shared.Religion.Nullrod.Systems;

public abstract partial class SharedWeakToHolySystem : EntitySystem
{
    [Dependency] private GoobBibleSystem _goobBible = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;

    [SubscribeLocalEvent]
    private void OnUnholyDamage(Entity<ShouldTakeHolyComponent> uid, ref DamageUnholyEvent args)
    {
        args.ShouldTakeHoly = true;
    }

    [SubscribeLocalEvent]
    private void OnMelee(Entity<BibleComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0 ||
            !TryComp(ent, out UseDelayComponent? useDelay) ||
            _useDelay.IsDelayed((ent, useDelay)) ||
            !HasComp<BibleUserComponent>(args.User))
            return;

        _goobBible.TryDoSmite(ent, args.User, args.HitEntities[0], useDelay);
    }

    [SubscribeLocalEvent]
    private void OnSmiteAttempt(Entity<AlwaysTakeHolyComponent> ent, ref BibleSmiteAttemptEvent args)
    {
        if (ent.Comp.ShouldBibleSmite)
            args.ShouldSmite = true;
    }

    [SubscribeLocalEvent]
    private void OnUserStatus(Entity<AlwaysTakeHolyComponent> ent, ref UserShouldTakeHolyEvent args)
    {
        if (ent.Comp.LifeStage > ComponentLifeStage.Running)
            return;

        args.WeakToHoly = true;
        args.ShouldTakeHoly = true;
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<AlwaysTakeHolyComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        var ev = new UnholyStatusChangedEvent(ent, ent, false);
        RaiseLocalEvent(ent, ref ev);
    }

    [SubscribeLocalEvent]
    private void OnInit(Entity<AlwaysTakeHolyComponent> ent, ref MapInitEvent args)
    {
        EnsureComp<WeakToHolyComponent>(ent);
        var ev = new UnholyStatusChangedEvent(ent, ent, true);
        RaiseLocalEvent(ent, ref ev);
    }

    [SubscribeLocalEvent]
    private void OnHolyDamageModify(Entity<BodyComponent> ent, ref DamageModifyEvent args)
    {
        var unholyEvent = new DamageUnholyEvent(args.Target, args.Origin);
        RaiseLocalEvent(args.Target, ref unholyEvent);

        var holyCoefficient = 0f; // Default resistance

        if (unholyEvent.ShouldTakeHoly)
            holyCoefficient = 1f; //Allow holy damage

        DamageModifierSet modifierSet = new()
        {
            Coefficients = new Dictionary<string, float>
            {
                { "Holy", holyCoefficient },
            },
        };

        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modifierSet);
    }
}
