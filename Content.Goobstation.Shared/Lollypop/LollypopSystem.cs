using Content.Shared.FixedPoint;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Clothing;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Lollypop;

public sealed class LollypopSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IngestionSystem _ingestion = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly FlavorProfileSystem _flavorProfile = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LollypopComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<EquippedLollypopComponent, ClothingGotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<EquippedLollypopComponent, EdibleEvent>(OnEdible);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted) // it'd probably cause sound spam
            return;

        var query = EntityQueryEnumerator<EquippedLollypopComponent, LollypopComponent>();
        var now = _timing.CurTime;
        while (query.MoveNext(out var uid, out var equipped, out var comp))
        {
            if (equipped.NextBite > now)
                continue;

            Eat((uid, comp, equipped));
        }
    }

    private void OnEquipped(Entity<LollypopComponent> ent, ref ClothingGotEquippedEvent args)
    {
        var equipped = EnsureComp<EquippedLollypopComponent>(ent);
        equipped.HeldBy = args.Wearer;
        equipped.NextBite = _timing.CurTime + ent.Comp.BiteInterval;
        Dirty(ent, equipped);

        // add popup of taste
        if (!TryComp<EdibleComponent>(ent.Owner, out var edible))
            return;
        if (!_solution.TryGetSolution(ent.Owner, edible.Solution, out var soln, out _))
            return;

        var flavors = _flavorProfile.GetLocalizedFlavorsMessage(args.Wearer, soln.Value.Comp.Solution);
        var proto = _proto.Index(edible.Edible);
        _popup.PopupClient(Loc.GetString(proto.Message, ("food", ent.Owner), ("flavors", flavors)), args.Wearer,args.Wearer);
    }

    private void OnUnequipped(Entity<EquippedLollypopComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        RemCompDeferred(ent, ent.Comp);
    }

    private void OnEdible(Entity<EquippedLollypopComponent> ent, ref EdibleEvent args)
    {
        // remove doafter when lollypop is being automatically eaten
        if (ent.Comp.InstantEat)
            args.Time = TimeSpan.Zero;
    }

    private void Eat(Entity<LollypopComponent, EquippedLollypopComponent> ent)
    {
        if (ent.Comp2.HeldBy is not {} user)
            return;

        ent.Comp2.InstantEat = true; // goida, can't override doafter in ingestion API
        _ingestion.TryIngest(user, ent);
        ent.Comp2.InstantEat = false;
        ent.Comp2.NextBite = TerminatingOrDeleted(ent)
            ? TimeSpan.Zero // lollypop is empty stop updating
            : _timing.CurTime + ent.Comp1.BiteInterval;
        Dirty(ent, ent.Comp2);
    }
}
