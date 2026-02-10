using Content.Shared.FixedPoint;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Clothing;
using Content.Shared.DoAfter;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Lollypop;

public sealed class LollypopSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
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
        SubscribeLocalEvent<EquippedLollypopComponent, BeforeIngestedEvent>(OnBeforeIngested);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // it causes popup spam
        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<EquippedLollypopComponent, LollypopComponent>();
        var now = _timing.CurTime;
        while (query.MoveNext(out var uid, out var equipped, out var comp))
        {
            if (equipped.NextBite is {} nextBite && nextBite <= now)
                Eat((uid, comp, equipped));
        }
    }

    private void OnEquipped(Entity<LollypopComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        var equipped = EnsureComp<EquippedLollypopComponent>(ent);
        var user = args.Wearer;
        equipped.HeldBy = user;
        equipped.NextBite = _timing.CurTime + ent.Comp.BiteInterval;
        Dirty(ent, equipped);

        // add popup of taste immediately
        TastePopup(ent, user, predicted: true);
    }

    private void OnUnequipped(Entity<EquippedLollypopComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        RemCompDeferred(ent, ent.Comp);
    }

    private void OnBeforeIngested(Entity<EquippedLollypopComponent> ent, ref BeforeIngestedEvent args)
    {
        if (args.Max > ent.Comp.MaxEaten);
            args.Max = ent.Comp.MaxEaten;
    }

    private void Eat(Entity<LollypopComponent, EquippedLollypopComponent> ent)
    {
        if (ent.Comp2.HeldBy is not {} user)
            return;

        // manually instantly eat a bite because there is no API and i cbf to refactor it
        var now = _timing.CurTime;
        var fakeArgs = _ingestion.GetEdibleDoAfterArgs(user, user, ent);
        var ev = new EatingDoAfterEvent()
        {
            DoAfter = new(0, fakeArgs, now)
        };
        RaiseLocalEvent(user, ev);
        // ingestion always assumes it's predicted so it doesnt do a popup itself
        TastePopup(ent, user, predicted: false);

        ent.Comp2.NextBite = TerminatingOrDeleted(ent)
            ? null // lollypop is empty stop updating
            : now + ent.Comp1.BiteInterval;
        Dirty(ent, ent.Comp2);
    }

    private void TastePopup(EntityUid uid, EntityUid user, bool predicted)
    {
        if (!TryComp<EdibleComponent>(uid, out var edible))
            return;
        if (!_solution.TryGetSolution(uid, edible.Solution, out _, out var soln))
            return;

        var flavors = _flavorProfile.GetLocalizedFlavorsMessage(user, soln);
        var proto = _proto.Index(edible.Edible);
        var msg = Loc.GetString(proto.Message, ("food", uid), ("flavors", flavors));
        if (predicted)
            _popup.PopupClient(msg, user, user);
        else
            _popup.PopupEntity(msg, user, user);
    }
}
