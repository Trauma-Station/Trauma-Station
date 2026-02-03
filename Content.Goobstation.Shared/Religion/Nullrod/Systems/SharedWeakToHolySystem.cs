using Content.Goobstation.Common.Religion;
using Content.Goobstation.Shared.Bible;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Timing;

namespace Content.Goobstation.Shared.Religion.Nullrod.Systems;

public abstract class SharedWeakToHolySystem : EntitySystem
{
    [Dependency] protected readonly SharedBodySystem Body = default!;

    [Dependency] private readonly GoobBibleSystem _goobBible = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly WoundSystem _wound = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShouldTakeHolyComponent, DamageUnholyEvent>(OnUnholyDamage);
        SubscribeLocalEvent<ShouldTakeHolyComponent, InteractUsingEvent>(AfterBibleUse);

        SubscribeLocalEvent<AlwaysTakeHolyComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<AlwaysTakeHolyComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<AlwaysTakeHolyComponent, UserShouldTakeHolyEvent>(OnUserStatus);
        SubscribeLocalEvent<AlwaysTakeHolyComponent, BibleSmiteAttemptEvent>(OnSmiteAttempt);

        SubscribeLocalEvent<DamageableComponent, DamageModifyEvent>(OnHolyDamageModify);
    }

    private void OnUnholyDamage(Entity<ShouldTakeHolyComponent> uid, ref DamageUnholyEvent args)
    {
        args.ShouldTakeHoly = true;
    }

    private void AfterBibleUse(Entity<ShouldTakeHolyComponent> ent, ref InteractUsingEvent args)
    {
        if (!HasComp<BibleComponent>(args.Used) ||
            !TryComp(args.Used, out UseDelayComponent? useDelay)
            || _useDelay.IsDelayed((args.Used, useDelay))
            || !HasComp<BibleUserComponent>(args.User))
            return;

        _goobBible.TryDoSmite(args.Used, args.User, args.Target, useDelay);
    }

    private void OnSmiteAttempt(Entity<AlwaysTakeHolyComponent> ent, ref BibleSmiteAttemptEvent args)
    {
        if (ent.Comp.ShouldBibleSmite)
            args.ShouldSmite = true;
    }

    private void OnUserStatus(Entity<AlwaysTakeHolyComponent> ent, ref UserShouldTakeHolyEvent args)
    {
        if (ent.Comp.LifeStage > ComponentLifeStage.Running)
            return;

        args.ShouldTakeHoly = true;
    }

    private void OnShutdown(Entity<AlwaysTakeHolyComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        var ev = new UnholyStatusChangedEvent(ent, ent, false);
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnInit(Entity<AlwaysTakeHolyComponent> ent, ref MapInitEvent args)
    {
        EnsureComp<WeakToHolyComponent>(ent);
        var ev = new UnholyStatusChangedEvent(ent, ent, true);
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnHolyDamageModify(Entity<DamageableComponent> ent, ref DamageModifyEvent args)
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

        if (!TryComp<BodyComponent>(ent, out var body))
            return;

        if (!Body.TryGetRootPart(ent, out var rootPart, body: body))
            return;

        foreach (var woundable in _wound.GetAllWoundableChildren(rootPart.Value))
        {
            if (HasComp<DamageableComponent>(woundable))
                args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modifierSet);
        }
    }
}
